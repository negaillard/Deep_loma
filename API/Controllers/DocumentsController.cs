
using API.Authorization;
using API.Helpers;
using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using MassTransit;
using MessageContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Models.Enums;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;
using System;
using System.IO;
using System.Security.Cryptography.Pkcs;

namespace API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class DocumentsController : ControllerBase
	{
		private readonly IDocumentLogic _documentLogic;
		private readonly IDocumentUserLogic _documentUserLogic;
		private readonly ISignatureStorage _signatureStorage;
		private readonly IUserLogic _userLogic;
		private readonly IFileStorage _fileStorage;
		private readonly ILogger<DocumentsController> _logger;
		private readonly IAntivirusService _antivirus;
		private readonly FileUploadPolicy _filePolicy;
		private readonly IPublishEndpoint _publishEndpoint;
		private readonly ICertificateLogic _certificateLogic;

		public DocumentsController(
			IDocumentLogic documentLogic,
			IDocumentUserLogic documentUserLogic,
			ISignatureStorage signatureStorage,
			IUserLogic userLogic,
			IFileStorage fileStorage,
			IAntivirusService antivirus,
			IOptions<FileUploadPolicy> filePolicy,
			ILogger<DocumentsController> logger,
			IPublishEndpoint publishEndpoint,
			ICertificateLogic certificateLogic)
		{
			_documentLogic = documentLogic;
			_documentUserLogic = documentUserLogic;
			_signatureStorage = signatureStorage;
			_userLogic = userLogic;
			_fileStorage = fileStorage;
			_antivirus = antivirus;
			_filePolicy = filePolicy.Value;
			_logger = logger;
			_publishEndpoint = publishEndpoint;
			_certificateLogic = certificateLogic;
		}

		[AuthorizeSigner]
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			try
			{
				_logger.LogInformation($"Попытка получения документа по id{id}");
				var document = await _documentLogic.ReadElementAsync(new DocumentSearchModel { Id = id });
				if (document == null)
				{
					_logger.LogWarning($"документ по id{id} не найден");
					return NotFound();
				}
				_logger.LogInformation($"документ по id{id} найден");
				return Ok(document);
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка получения документа " + ex.Message);
			}
		}

	[AuthorizeDocument]
	[HttpPost]
	[RequestSizeLimit(100_000_000)]
	public async Task<IActionResult> Create(
		[FromForm] string title,
		[FromForm] string description,
		[FromForm] List<int> userIds,
		[FromForm] bool isSequential = false,
		IFormFile? file = null)
		{
			try
			{
				if (file == null || file.Length == 0)
				{
					return BadRequest("Не передан файл документа");
				}

				var user = HttpContext.Items["User"] as UserViewModel;
				if (user == null)
				{
					return Unauthorized("Требуется авторизация");
				}
				/// проверка расширений
				string extension = Path.GetExtension(file.FileName).ToLower();

				if (!_filePolicy.AllowedExtensions.Contains(extension))
				{
					_logger.LogWarning("Попытка загрузки запрещенного типа файла: {Ext}", extension);
					return BadRequest("Тип файла запрещён политикой безопасности");
				}

				/// проверка на вирусы
				using var stream = file.OpenReadStream();

				_logger.LogInformation("Проверка файла на вирусы");

				if (!await _antivirus.IsFileCleanAsync(stream))
				{
					_logger.LogWarning("Вирус обнаружен в файле {FileName}", file.FileName);
					return BadRequest("Файл содержит вирус");
				}

				stream.Position = 0;
				/// проверка закончена


				var model = new DocumentBindingModel
				{
					Title = title,
					Description = description,
					UserIds = userIds,
					CreatedByUserId = user.Id,
					CreatedAt = DateTime.UtcNow,
					Status = DocumentStatus.NOT_SIGNED,
					IsDeleted = false,
					IsSequential = isSequential,
				};

				_logger.LogInformation("Попытка создания документа '{Title}'", model.Title);

				if (!await _documentLogic.CreateAsync(model, stream, extension))
				{
					_logger.LogWarning("Документ '{Title}' не был создан", model.Title);
					return BadRequest("Ошибка при создании документа");
				}
				_logger.LogInformation("Документ '{Title}' успешно создан", model.Title);

				// отправка уведомлений
				await PublishDocumentCreatedNotificationsAsync(model, user, isSequential);

				return Ok("Документ создан");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при создании документа " + ex.Message);
			}
		}

		// логика отправки уведомлений на consumer
		private async Task PublishDocumentCreatedNotificationsAsync(
					DocumentBindingModel model,
					UserViewModel creator,
					bool isSequential)
		{
			var signerIds = isSequential
				? model.UserIds.Take(1)
				: model.UserIds;

			foreach (var userId in signerIds)
			{
				var signer = await _userLogic.ReadElementAsync(new UserSearchModel { Id = userId });
				if (signer == null || string.IsNullOrWhiteSpace(signer.Email))
				{
					_logger.LogWarning("Не найден email подписанта {UserId}", userId);
					continue;
				}

				await _publishEndpoint.Publish(new NotificationMessage(
					RecipientEmail: signer.Email,
					RecipientName: signer.Fullname,
					DocumentTitle: model.Title,
					RequestedByName: creator.Fullname,
					RequestedAt: DateTime.UtcNow));
			}
		}

		[AuthorizeDocument]
		[HttpPut]
		public async Task<IActionResult> Update([FromBody] DocumentBindingModel model)
		{
			try
			{
				if (!await _documentLogic.UpdateAsync(model))
				{
					_logger.LogWarning($"Документ c id{model.Id} не был обновлен");
					return BadRequest("Ошибка при обновлении документа");
				}
				_logger.LogInformation($"Документ c id{model.Id} был обновлен");
				return Ok("Документ обновлён");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при обновлении документа " + ex.Message);
			}
		}

		[AuthorizeDocument]
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				if (!await _documentLogic.DeleteAsync(new DocumentBindingModel { Id = id }))
				{
					_logger.LogWarning($"Документ c id{id} не был удален");
					return BadRequest("Ошибка при удалении документа");
				}
				_logger.LogInformation($"документ c id{id} был удален");
				return Ok("Документ удалён");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при удалении документа " + ex.Message);
			}
		}

		/// <summary>
		/// Фильтрация документов с пагинацией. Параметры statuses (несколько раз в query) задают OR по статусам;
		/// если не указаны — одиночный status или без фильтра по статусу.
		/// </summary>
		[AuthorizeSigner]
		[HttpGet("filter")]
		public async Task<IActionResult> Filter(
			[FromQuery] string? title = null,
			[FromQuery] string? search = null,
			[FromQuery] int? createdByUserId = null,
			[FromQuery] DocumentStatus? status = null,
			[FromQuery] DocumentStatus[]? statuses = null,
			[FromQuery] bool? isDeleted = null,
			[FromQuery] int pageNumber = 1,
			[FromQuery] int pageSize = 20)
		{
			_logger.LogInformation(
				"Фильтрация документов (стр. {Page}, размер {Size})",
				pageNumber, pageSize);

			var model = new DocumentSearchModel
			{
				Title = title,
				SearchText = search,
				CreatedByUserId = createdByUserId,
				Status = status,
				Statuses = statuses != null && statuses.Length > 0 ? [.. statuses] : null,
				IsDeleted = isDeleted,
				PageNumber = pageNumber,
				PageSize = pageSize
			};

			var result = await _documentLogic.ReadFilteredPagedAsync(model);
			return Ok(result);
		}

		[AuthorizeSigner]
		[HttpGet("{id}/file")]
		public async Task<IActionResult> GetFile(int id)
		{
			try
			{
				_logger.LogInformation($"Попытка скачать файл документа id{id}");
				var document = await _documentLogic.ReadElementAsync(new DocumentSearchModel { Id = id });
				if (document == null || string.IsNullOrWhiteSpace(document.Path))
				{
					return NotFound("Файл документа не найден");
				}

				var stream = await _fileStorage.GetFileAsync(document.Path);
				var fileName = Path.GetFileName(document.Path);
				return File(stream, "application/octet-stream", fileName);
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при скачивании документа " + ex.Message);
			}
		}

	/// ПОЛУЧЕНИЕ ТОЛЬКО ДОКУМЕНТОВ НА ПОДПИСАНИЕ. ДЛЯ КЛИЕНТСКИХ ПРИЛОЖЕНИЙ. НЕ ТРОГАТЬ РАБОТАЕТ
	[AuthorizeSigner]
	[HttpGet("get-for-sign")]
	public async Task<IActionResult> GetDocumentsForSign(
		[FromQuery] SigningStatus? signingStatus = null,
		[FromQuery] int pageNumber = 1,
		[FromQuery] int pageSize = 10)
	{
		try
		{
			var user = HttpContext.Items["User"] as UserViewModel;
			if (user == null)
				return Unauthorized();

			if (pageNumber < 1) pageNumber = 1;
			if (pageSize < 1 || pageSize > 100) pageSize = 10;

			_logger.LogInformation(
				"Получение документов для подписания пользователем {UserId} (стр. {Page}, размер {Size})",
				user.Id, pageNumber, pageSize);

			var result = await _documentUserLogic.GetPagedForSignAsync(
				user.Id, signingStatus, pageNumber, pageSize);

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при получении документов для подписания");
			return BadRequest("Ошибка при получении документов: " + ex.Message);
		}
	}

	[AuthorizeDocument]
	[HttpGet("{id}/verification-package")]
	public async Task<IActionResult> GetVerificationPackage(int id)
	{
		try
		{
			var document = await _documentLogic.ReadElementAsync(new DocumentSearchModel { Id = id });
			if (document == null || document.IsDeleted)
				return NotFound("Документ не найден");

			if (document.Status != DocumentStatus.SIGNED)
				return BadRequest("Пакет верификации доступен только для полностью подписанных документов");

			var safeFolderName = FileStorage.LocalFileStorage.SanitizeFolderName(document.Title);
			var relativeZipPath = $"documents/{safeFolderName}/verification_{id}.zip";

			if (await _fileStorage.FileExistsAsync(relativeZipPath))
			{
				_logger.LogInformation("Возврат кэшированного пакета верификации для документа {Id}", id);
				var cachedZipStream = await _fileStorage.GetFileAsync(relativeZipPath);
				var safeTitleName = SanitizeName(document.Title);
				return File(cachedZipStream, "application/zip", $"verification_{safeTitleName}_{id}.zip");
			}

			var signatures = await _signatureStorage.GetFilteredListAsync(
				new SignatureSearchModel { DocumentId = id });

			if (signatures == null || signatures.Count == 0)
				return NotFound("Подписи для документа не найдены");

			_logger.LogInformation("Генерация нового пакета верификации для документа {Id}", id);
			var zipBytes = await VerificationPackageHelper.GenerateVerificationPackageZipAsync(
				document,
				signatures,
				_fileStorage,
				_userLogic,
				_certificateLogic,
				_logger);

			// Сохраняем сформированный пакет верификации в файловое хранилище для кэширования
			try
			{
				_logger.LogInformation("Кэширование пакета верификации для документа {Id} в {RelativeZipPath}", id, relativeZipPath);
				using (var ms = new MemoryStream(zipBytes))
				{
					await _fileStorage.SaveVerificationPackageAsync(id, document.Title, ms);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Не удалось сохранить пакет верификации в кэш для документа {Id}", id);
			}

			var safeTitle = SanitizeName(document.Title);
			return File(zipBytes,
				"application/zip",
				$"verification_{safeTitle}_{id}.zip");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при формировании пакета верификации для документа {Id}", id);
			return BadRequest("Ошибка при формировании пакета: " + ex.Message);
		}
	}

	// вспомогательные
	private static string SanitizeName(string name)
	{
		var invalid = Path.GetInvalidFileNameChars();
		return new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
	}
}

public class FileUploadPolicy
	{
		public long MaxFileSize { get; set; }
		public List<string> AllowedExtensions { get; set; } = new();
	}
}
