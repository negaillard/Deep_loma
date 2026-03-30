using API.Authorization;
using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using MassTransit;
using MessageContracts;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Security.Cryptography.Pkcs;

namespace API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SigningController : ControllerBase
	{
		private readonly IDocumentUserLogic _documentUserLogic;
		private readonly IUserLogic _userLogic;
		private readonly IPublishEndpoint _publishEndpoint;
		private readonly ISignatureStorage _signatureStorage;
		private readonly IFileStorage _fileStorage;
		private readonly ILogger<SigningController> _logger;

		public SigningController(
			IDocumentUserLogic documentUserLogic,
			IUserLogic userLogic,
			IPublishEndpoint publishEndpoint,
			ISignatureStorage signatureStorage,
			IFileStorage fileStorage,
			ILogger<SigningController> logger)
		{
			_documentUserLogic = documentUserLogic;
			_userLogic = userLogic;
			_publishEndpoint = publishEndpoint;
			_signatureStorage = signatureStorage;
			_fileStorage = fileStorage;
			_logger = logger;
		}

		[AuthorizeSigner]
		[HttpGet("{id}/signers")]
		public async Task<IActionResult> GetSigners(int id)
		{
			_logger.LogInformation("Получение подписантов для документа {DocumentId}", id);
			var documentUsers = await _documentUserLogic.ReadListAsync(
				new DocumentUserSearchModel { DocumentId = id });

			if (documentUsers == null)
				return Ok(new List<object>());

			foreach (var du in documentUsers)
			{
				var user = await _userLogic.ReadElementAsync(new UserSearchModel { Id = du.UserId });
				du.UserFullname = user?.Fullname;
			}

			return Ok(documentUsers);
		}

		/// ИСПОЛЬЗУЕТСЯ ДЛЯ ВНУТРЕННЕЙ ПОДПИСИ
		[AuthorizeSigner]
		[HttpPost("{id}/sign-intent")]
		public async Task<IActionResult> SignIntent(int id)
		{
			try
			{
				var user = HttpContext.Items["User"] as UserViewModel;
				if (user == null)
					return Unauthorized();

				_logger.LogInformation("Пользователь {UserId} выразил намерение подписать документ {DocumentId}", user.Id, id);

				var documentUser = await _documentUserLogic.ReadElementAsync(new DocumentUserSearchModel
				{
					UserId = user.Id,
					DocumentId = id
				});

				if (documentUser == null)
					return NotFound("Пользователь не назначен на документ");

				if (documentUser.SigningStatus == SigningStatus.SIGNED)
					return BadRequest("Документ уже подписан");

				if (documentUser.SigningStatus == SigningStatus.DECLINED)
					return BadRequest("Нельзя подписать документ после отказа");

				if (documentUser.SigningStatus == SigningStatus.PENDING)
					return BadRequest("Подпись уже в процессе обработки");

				if (user.CertificateId <= 0)
					return BadRequest("У пользователя нет активного сертификата");

				// Order > 1 означает что документ последовательный и пользователь не первый в очереди
				if (documentUser.Order > 1)
				{
					var allSigners = await _documentUserLogic.ReadListAsync(
						new DocumentUserSearchModel { DocumentId = id });

					var hasUnfinishedPrevious = allSigners?.Any(du =>
						du.Order < documentUser.Order &&
						du.SigningStatus != SigningStatus.SIGNED) ?? false;

					if (hasUnfinishedPrevious)
						return BadRequest("Ещё не все предыдущие подписанты подписали документ");
				}

			var updated = new DocumentUserBindingModel
			{
				Id = documentUser.Id,
				UserId = documentUser.UserId,
				DocumentId = documentUser.DocumentId,
				AssignedAt = DateTime.UtcNow,
				SigningStatus = SigningStatus.PENDING,
				Order = documentUser.Order
			};

				if (!await _documentUserLogic.UpdateAsync(updated))
					return BadRequest("Ошибка при обновлении статуса");

				await _publishEndpoint.Publish(new SigningRequestMessage(
					DocumentId: id,
					UserId: user.Id,
					RequestedAt: DateTime.UtcNow));

				_logger.LogInformation("Запрос на подписание документа {DocumentId} пользователем {UserId} отправлен в очередь", id, user.Id);
				// 202
				return StatusCode(202, "Запрос на подписание принят в обработку");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при обработке намерения подписать документ {DocumentId}", id);
				return BadRequest("Ошибка при подписании документа: " + ex.Message);
			}
		}

		/// <summary>
		/// ОТКАЗ ОТ ПОДПИСИ
		/// </summary>
		[AuthorizeSigner]
		[HttpPost("{id}/reject")]
		public async Task<IActionResult> Reject(int id)
		{
			try
			{
				var user = HttpContext.Items["User"] as UserViewModel;
				if (user == null)
					return Unauthorized();

				_logger.LogInformation("Пользователь {UserId} отказался от подписи документа {DocumentId}", user.Id, id);

				var documentUser = await _documentUserLogic.ReadElementAsync(new DocumentUserSearchModel
				{
					UserId = user.Id,
					DocumentId = id
				});

				if (documentUser == null)
					return NotFound("Пользователь не назначен на документ");

				if (documentUser.SigningStatus == SigningStatus.SIGNED)
					return BadRequest("Нельзя отказаться после подписания");

			var updated = new DocumentUserBindingModel
			{
				Id = documentUser.Id,
				UserId = documentUser.UserId,
				DocumentId = documentUser.DocumentId,
				AssignedAt = documentUser.AssignedAt,
				SigningStatus = SigningStatus.DECLINED,
				Order = documentUser.Order
			};

				if (!await _documentUserLogic.UpdateAsync(updated))
					return BadRequest("Ошибка при отказе от подписи");

				return Ok("Отказ зафиксирован");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при отказе от подписи документа {DocumentId}", id);
				return BadRequest("Ошибка при отказе от подписи: " + ex.Message);
			}
		}

		/// <summary>
		/// Локальный режим: клиент присылает готовую подпись (base64 PKCS#7 detached).
		/// Сервер сохраняет подпись, извлекает из неё публичный сертификат и отмечает документ подписанным.
		/// </summary>
		[AuthorizeSigner]
		[HttpPost("{id}/submit-signature")]
		public async Task<IActionResult> SubmitSignature(int id, [FromBody] SubmitSignatureRequest request)
		{
			try
			{
				var user = HttpContext.Items["User"] as UserViewModel;
				if (user == null)
					return Unauthorized();

				_logger.LogInformation("Пользователь {UserId} отправляет локальную подпись для документа {DocumentId}", user.Id, id);

				if (string.IsNullOrWhiteSpace(request.SignatureBase64))
					return BadRequest("Подпись не передана");

				byte[] signatureBytes;
				try
				{
					signatureBytes = Convert.FromBase64String(request.SignatureBase64);
				}
				catch
				{
					return BadRequest("Некорректный формат base64");
				}

				var documentUser = await _documentUserLogic.ReadElementAsync(new DocumentUserSearchModel
				{
					UserId = user.Id,
					DocumentId = id
				});

				if (documentUser == null)
					return NotFound("Пользователь не назначен на документ");

				if (documentUser.SigningStatus == SigningStatus.SIGNED)
					return BadRequest("Документ уже подписан");

				if (documentUser.SigningStatus == SigningStatus.DECLINED)
					return BadRequest("Нельзя подписать документ после отказа");

				if (documentUser.Order > 1)
				{
					var allSigners = await _documentUserLogic.ReadListAsync(
						new DocumentUserSearchModel { DocumentId = id });

					var hasUnfinishedPrevious = allSigners?.Any(du =>
						du.Order < documentUser.Order &&
						du.SigningStatus != SigningStatus.SIGNED) ?? false;

					if (hasUnfinishedPrevious)
						return BadRequest("Ещё не все предыдущие подписанты подписали документ");
				}

				// Создаём запись подписи без пути (нужен ID для имени файла)
				var sigRecord = await _signatureStorage.InsertAsync(new SignatureBindingModel
				{
					UserId = user.Id,
					DocumentId = id,
					CerificateId = 0,
					SignedAt = DateTime.UtcNow,
					SignatureValue = string.Empty,
					Path = string.Empty,
					CertificatePath = string.Empty
				});

				if (sigRecord == null)
					return BadRequest("Не удалось создать запись подписи");

				// Сохраняем .sig файл
				using var sigStream = new MemoryStream(signatureBytes);
				var sigPath = await _fileStorage.SaveSignatureAsync(id, sigRecord.Id, sigStream);

				// Извлекаем публичный сертификат из PKCS#7
				string certPath = string.Empty;
				try
				{
					var signedCms = new SignedCms();
					signedCms.Decode(signatureBytes);
					if (signedCms.SignerInfos.Count > 0)
					{
						var cert = signedCms.SignerInfos[0].Certificate;
						if (cert != null)
						{
							var cerBytes = cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert);
							certPath = await _fileStorage.SaveSignatureCertificateAsync(id, sigRecord.Id, cerBytes);
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Не удалось извлечь сертификат из подписи документа {DocumentId}", id);
				}

				// Обновляем запись с путями
				await _signatureStorage.UpdateAsync(new SignatureBindingModel
				{
					Id = sigRecord.Id,
					UserId = user.Id,
					DocumentId = id,
					CerificateId = 0,
					SignedAt = sigRecord.SignedAt,
					SignatureValue = string.Empty,
					Path = sigPath,
					CertificatePath = certPath
				});

				// Помечаем DocumentUser как SIGNED
				var updatedDu = new DocumentUserBindingModel
				{
					Id = documentUser.Id,
					UserId = documentUser.UserId,
					DocumentId = documentUser.DocumentId,
					AssignedAt = documentUser.AssignedAt,
					SigningStatus = SigningStatus.SIGNED,
					Order = documentUser.Order
				};

				if (!await _documentUserLogic.UpdateAsync(updatedDu))
					return BadRequest("Ошибка при обновлении статуса подписи");

				// Уведомляем следующего подписанта (если последовательная подпись)
				var allDu = await _documentUserLogic.ReadListAsync(
					new DocumentUserSearchModel { DocumentId = id });

				if (allDu != null)
				{
					var nextSigner = allDu
						.Where(du => du.Order > documentUser.Order && du.SigningStatus == SigningStatus.NOT_SIGNED)
						.OrderBy(du => du.Order)
						.FirstOrDefault();

					if (nextSigner != null)
					{
						await _publishEndpoint.Publish(new NotificationMessage(
							UserId: nextSigner.UserId,
							Title: $"Документ #{id} готов к подписанию",
							RequestedAt: DateTime.UtcNow));
					}
				}

				_logger.LogInformation("Локальная подпись документа {DocumentId} от пользователя {UserId} успешно сохранена", id, user.Id);
				return Ok(new { message = "Подпись успешно сохранена", signatureId = sigRecord.Id });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при сохранении локальной подписи документа {DocumentId}", id);
				return BadRequest("Ошибка при сохранении подписи: " + ex.Message);
			}
		}
	}

	public record SubmitSignatureRequest(string SignatureBase64);
}
