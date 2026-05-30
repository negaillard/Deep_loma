using API.Authorization;
using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CertificatesController : ControllerBase
	{
		private readonly ICertificateLogic _certificateLogic;
		private readonly IUserLogic _userLogic;
		private readonly IConfiguration _configuration;
		private readonly ILogger<CertificatesController> _logger;

		public CertificatesController(
			ICertificateLogic certificateLogic,
			IUserLogic userLogic,
			IConfiguration configuration,
			ILogger<CertificatesController> logger)
		{
			_certificateLogic = certificateLogic;
			_userLogic = userLogic;
			_configuration = configuration;
			_logger = logger;
		}

		/// <summary>
		/// Генерирует самоподписанный сертификат для указанного пользователя.
		/// Owner (CN) берётся из ФИО пользователя в БД.
		/// Publisher (O) берётся из настройки Organization в конфиге.
		/// Доступно только администратору.
		/// </summary>
		[AuthorizeAdmin]
		[HttpPost("{userId}/generate")]
		public async Task<IActionResult> Generate(int userId)
		{
			try
			{
				_logger.LogInformation("Генерация сертификата для пользователя id={UserId}", userId);

				var user = await _userLogic.ReadElementAsync(new UserSearchModel { Id = userId });
				if (user == null)
				{
					return NotFound($"Пользователь id={userId} не найден");
				}

				var organization = _configuration["Organization"]
					?? throw new InvalidOperationException("Параметр Organization не задан в конфигурации");

				var certificate = await _certificateLogic.GenerateSelfSignedAsync(
					userId,
					owner: user.Fullname,
					publisher: organization);

				if (certificate == null)
				{
					_logger.LogWarning("Сертификат для пользователя id={UserId} не был создан", userId);
					return BadRequest("Не удалось создать сертификат");
				}

				_logger.LogInformation("Сертификат id={CertId} для пользователя id={UserId} ({Fullname}) успешно создан",
					certificate.Id, userId, user.Fullname);

				return Ok(certificate);
			}
			catch (ArgumentException ex)
			{
				_logger.LogWarning(ex, "Ошибка валидации при генерации сертификата");
				return BadRequest(ex.Message);
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogWarning(ex, "Конфликт при генерации сертификата для пользователя id={UserId}", userId);
				return Conflict(ex.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Непредвиденная ошибка при генерации сертификата");
				return StatusCode(500, "Внутренняя ошибка сервера");
			}
		}

		/// <summary>
		/// Возвращает метаданные сертификата по его идентификатору.
		/// Доступно всем авторизованным пользователям.
		/// </summary>
		[AuthorizeSigner]
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			try
			{
				_logger.LogInformation("Получение сертификата id={CertId}", id);
				var currentUser = HttpContext.Items["User"] as Contracts.ViewModels.UserViewModel;
				if (currentUser == null)
				{
					return Unauthorized();
				}

				var certificate = await _certificateLogic.ReadElementAsync(new CertificateSearchModel { Id = id });

				if (certificate == null)
				{
					_logger.LogWarning("Сертификат id={CertId} не найден", id);
					return NotFound();
				}

				// Проверка прав доступа (BOLA/IDOR)
				if (currentUser.SystemRole != Models.Enums.SystemRole.SystemAdmin && 
				    currentUser.SystemRole != Models.Enums.SystemRole.DocumentManager && 
				    certificate.UserId != currentUser.Id)
				{
					_logger.LogWarning("Пользователь {UserId} пытался получить чужой сертификат {CertId}", currentUser.Id, id);
					return Forbid();
				}

				return Ok(certificate);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при получении сертификата id={CertId}", id);
				return BadRequest(ex.Message);
			}
		}

		/// <summary>
		/// Удаляет сертификат по его идентификатору.
		/// Доступно только администратору.
		/// </summary>
		[AuthorizeAdmin]
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				_logger.LogInformation("Удаление сертификата id={CertId}", id);

				if (!await _certificateLogic.DeleteAsync(new CertificateBindingModel { Id = id }))
				{
					_logger.LogWarning("Сертификат id={CertId} не был удалён", id);
					return BadRequest("Не удалось удалить сертификат");
				}

				_logger.LogInformation("Сертификат id={CertId} успешно удалён", id);
				return Ok("Сертификат удалён");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при удалении сертификата id={CertId}", id);
				return BadRequest(ex.Message);
			}
		}
	}
}
