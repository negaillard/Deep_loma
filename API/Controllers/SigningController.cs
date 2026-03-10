using API.Authorization;
using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Models;
using System;

namespace API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SigningController : ControllerBase
	{
		private readonly IDocumentUserLogic _documentUserLogic;
		private readonly IUserLogic _userLogic;
		private readonly ILogger<SigningController> _logger;

		public SigningController(
			IDocumentUserLogic documentUserLogic,
			IUserLogic userLogic,
			ILogger<SigningController> logger)
		{
			_documentUserLogic = documentUserLogic;
			_userLogic = userLogic;
			_logger = logger;
		}

		[AuthorizeSigner]
		[HttpGet("{id}/signers")]
		public async Task<IActionResult> GetSigners(int id)
		{
			_logger.LogInformation($"Получение подписантов для документа {id}");
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
				{
					return Unauthorized();
				}

				_logger.LogInformation($"Пользователь {user.Id} подписывает документ {id}");
				var documentUser = await _documentUserLogic.ReadElementAsync(new DocumentUserSearchModel
				{
					UserId = user.Id,
					DocumentId = id
				});

				if (documentUser == null)
				{
					return NotFound("Пользователь не назначен на документ");
				}

				var updated = new DocumentUserBindingModel
				{
					Id = documentUser.Id,
					UserId = documentUser.UserId,
					DocumentId = documentUser.DocumentId,
					AssignedAt = documentUser.AssignedAt,
					SigningStatus = SigningStatus.SIGNED
				};

				if (!await _documentUserLogic.UpdateAsync(updated))
				{
					return BadRequest("Ошибка при подписании документа");
				}

				return Ok("Подпись зафиксирована");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при подписании документа " + ex.Message);
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
				{
					return Unauthorized();
				}

				_logger.LogInformation($"Пользователь {user.Id} отказался от подписи документа {id}");
				var documentUser = await _documentUserLogic.ReadElementAsync(new DocumentUserSearchModel
				{
					UserId = user.Id,
					DocumentId = id
				});

				if (documentUser == null)
				{
					return NotFound("Пользователь не назначен на документ");
				}

				var updated = new DocumentUserBindingModel
				{
					Id = documentUser.Id,
					UserId = documentUser.UserId,
					DocumentId = documentUser.DocumentId,
					AssignedAt = documentUser.AssignedAt,
					SigningStatus = SigningStatus.DECLINED
				};

				if (!await _documentUserLogic.UpdateAsync(updated))
				{
					return BadRequest("Ошибка при отказе от подписи");
				}

				return Ok("Отказ зафиксирован");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при отказе от подписи " + ex.Message);
			}
		}
	}
}
