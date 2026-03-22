using API.Authorization;
using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.ViewModels;
using MassTransit;
using MessageContracts;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SigningController : ControllerBase
	{
		private readonly IDocumentUserLogic _documentUserLogic;
		private readonly IUserLogic _userLogic;
		private readonly IPublishEndpoint _publishEndpoint;
		private readonly ILogger<SigningController> _logger;

		public SigningController(
			IDocumentUserLogic documentUserLogic,
			IUserLogic userLogic,
			IPublishEndpoint publishEndpoint,
			ILogger<SigningController> logger)
		{
			_documentUserLogic = documentUserLogic;
			_userLogic = userLogic;
			_publishEndpoint = publishEndpoint;
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
	}
}
