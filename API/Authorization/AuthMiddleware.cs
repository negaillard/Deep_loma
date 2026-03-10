using Contracts.LogicContracts;
using Contracts.LogicContracts.Authentication;
using Contracts.SearchModels;

namespace API.Authorization
{
	// класс проверяет валидность сессии и сохраняет пользователя, полученного по токену сессии в HttpContext.Items["User"]
	// HttpContext - это словарь в рамках одного http запроса. 
	// после того, как извлеченный по сессии пользователь окажется там, с помощью атрибутов сравниваются права (роли)
	// и принимается решение продолжать операцию или вернуть 403 ошибку.
	public class AuthMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger _logger;

		public AuthMiddleware(ILogger<AuthMiddleware> logger, RequestDelegate next)
		{
			_logger = logger;
			_next = next;
		}

		public async Task Invoke(HttpContext context, ISessionService sessionService, IUserLogic userLogic)
		{
			_logger.LogWarning("AUTH MIDDLEWARE START: {Method} {Path}", context.Request.Method, context.Request.Path);
			try
			{
				if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
				{
					var token = authHeader.ToString().Replace("Bearer ", "");
					_logger.LogInformation(">>> TOKEN: {Token}", token);

					var session = await sessionService.GetSessionAsync(token);

					if (session != null && session.IsActive && session.ExpiresAt > DateTime.UtcNow)
					{
						var user = await userLogic.ReadElementAsync(new UserSearchModel { Id = session.UserId });
						_logger.LogInformation("USER FOUND: {Found} | Role: {Role}", user != null, user?.SystemRole);
						if (user != null && user.IsActive)
						{
							context.Items["User"] = user;
							_logger.LogInformation("USER PLACED IN CONTEXT");
						}
					}
				}

				_logger.LogWarning("AUTH MIDDLEWARE: calling next");
				await _next(context);
				_logger.LogWarning("AUTH MIDDLEWARE: next completed");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "AUTH MIDDLEWARE EXCEPTION: {Message}", ex.Message);
				throw;
			}
		}
	}
}
