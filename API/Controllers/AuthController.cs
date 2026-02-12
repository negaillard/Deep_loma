using Contracts.LogicContracts;
using Contracts.LogicContracts.Authentication;
using Contracts.Requests;
using Contracts.Responses;
using Contracts.SearchModels;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly IUserLogic _userLogic;
		private readonly ICodeVerificationLogic _codeVerificationLogic;
		private readonly ISessionService _sessionService;
		private readonly ILogger<AuthController> _logger;

		public AuthController(
		  IUserLogic userLogic,
		  ICodeVerificationLogic codeVerificationLogic,
		  ISessionService sessionService,
		  ILogger<AuthController> logger)
		{
			_userLogic = userLogic;
			_codeVerificationLogic = codeVerificationLogic;
			_sessionService = sessionService;
			_logger = logger;
		}

		[HttpPost("send-login-code")]
		public async Task<IActionResult> SendLoginCode([FromBody] LoginRequest request)
		{
			try
			{
				_logger.LogInformation($"Запрос кода для входа: {request.Login}");
				var user = await _userLogic.ReadElementAsync(new UserSearchModel
				{
					Login = request.Login
				});

				if (user == null)
				{
					return BadRequest(new { error = "Пользователь с таким логином не найден" });
				}

				if (!IsAppTypeAllowed(user.SystemRole, request.appType))
				{
					return BadRequest(new { error = "Нет доступа к выбранному приложению" });
				}

				var result = await _codeVerificationLogic.SendCodeAsync(
					user.Email
					);

				if (!result.success)
				{
					return BadRequest(new { error = result.message });
				}

				_logger.LogInformation($"Код входа отправлен на: {user.Email}");
				return Ok(new MessageResponse { Message = result.message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при отправке кода входа");
				return BadRequest(new { error = "Ошибка сервера" });
			}
		}

		private static bool IsAppTypeAllowed(SystemRole role, AppType appType)
		{
			return role switch
			{
				SystemRole.SystemAdmin => true,
				SystemRole.Signer => appType == AppType.SIGNER_APP,
				SystemRole.DocumentManager => appType == AppType.DOCUMENT_APP || appType == AppType.SIGNER_APP,
				_ => false
			};
		}

		[HttpPost("verify-login")]
		public async Task<IActionResult> VerifyLogin([FromBody] VerifyLoginRequest request)
		{
			try
			{
				_logger.LogInformation($"Подтверждение входа: {request.Login}");
				var user = await _userLogic.ReadElementAsync(new UserSearchModel
				{
					Login = request.Login
				});

				if (user == null)
				{
					return BadRequest(new { error = "Ошибка при получении пользователя" });
				}

				var codeResult = await _codeVerificationLogic.VerifyCodeAsync(user.Email, request.Code);

				if (!codeResult.success)
				{
					return BadRequest(new { error = codeResult.message });
				}

				var sessionId = await _sessionService.CreateSessionAsync(user.Id, user.Login);

				_logger.LogInformation($"Успешный вход: {request.Login}, session: {sessionId}");

				return Ok(new VerifyLoginResponse
				{
					Message = "Вход выполнен успешно",
					Login = user.Login,
					UserId = user.Id,
					SessionToken = sessionId,
					SystemRole = user.SystemRole
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при подтверждении входа");
				return BadRequest(new { error = "Ошибка сервера" });
			}
		}

		[HttpPost("logout")]
		public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
		{
			await _sessionService.DeleteSessionAsync(request.SessionToken);
			return Ok(new MessageResponse { Message = "Выход выполнен" });
		}

		[HttpGet("validate-session")]
		public async Task<IActionResult> ValidateSession([FromHeader] string authorization)
		{
			var sessionId = authorization?.Replace("Bearer ", "");
			if (string.IsNullOrEmpty(sessionId))
				return Unauthorized();

			var isValid = await _sessionService.ValidateSessionAsync(sessionId);
			if (!isValid.Item1)
				return Unauthorized();

			return Ok(new ValidateSessionResponse { IsValid = true, Login = isValid.Item2 });
		}
	}
}
