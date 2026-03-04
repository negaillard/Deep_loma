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
		private readonly ILogger<CertificatesController> _logger;

		public CertificatesController(ICertificateLogic certificateLogic, ILogger<CertificatesController> logger)
		{
			_certificateLogic = certificateLogic;
			_logger = logger;
		}

		/// <summary>
		/// Генерирует самоподписанный сертификат для указанного пользователя.
		/// Доступно только администратору.
		/// </summary>
		[AuthorizeAdmin]
		[HttpPost("{userId}/generate")]
		public async Task<IActionResult> Generate(int userId, [FromBody] GenerateCertificateRequest request)
		{
			try
			{
				_logger.LogInformation("Генерация сертификата для пользователя id={UserId}", userId);

				var certificate = await _certificateLogic.GenerateSelfSignedAsync(
					userId,
					request.Owner,
					request.Publisher);

				if (certificate == null)
				{
					_logger.LogWarning("Сертификат для пользователя id={UserId} не был создан", userId);
					return BadRequest("Не удалось создать сертификат");
				}

				_logger.LogInformation("Сертификат id={CertId} для пользователя id={UserId} успешно создан",
					certificate.Id, userId);

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

				var certificate = await _certificateLogic.ReadElementAsync(new CertificateSearchModel { Id = id });

				if (certificate == null)
				{
					_logger.LogWarning("Сертификат id={CertId} не найден", id);
					return NotFound();
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

	public class GenerateCertificateRequest
	{
		/// <summary>
		/// ФИО или наименование владельца сертификата (поле CN в DN).
		/// </summary>
		public string Owner { get; set; } = string.Empty;

		/// <summary>
		/// Наименование организации-издателя (поле O в DN).
		/// </summary>
		public string Publisher { get; set; } = string.Empty;
	}
}
