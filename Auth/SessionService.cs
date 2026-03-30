using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Contracts.BindingModels.Authentication;
using Contracts.LogicContracts.Authentication;
namespace Auth
{
	public class SessionService : ISessionService
	{
		private readonly IDistributedCache _cache;
		//private readonly IConfiguration _config;
		private readonly string _sessionPrefix = "session:";

		public SessionService(IDistributedCache cache
			//IConfiguration config
			)

		{
			_cache = cache;
			//_config = config;
		}

		public async Task<string> CreateSessionAsync(int userId, string username)
		{
			var sessionId = GenerateSessionId();
			var session = new UserSession
			{
				SessionId = sessionId,
				UserId = userId,
				Username = username,
				CreatedAt = DateTime.UtcNow,
				ExpiresAt = DateTime.UtcNow.AddHours(24), // 24 часа
				IsActive = true
			};

			var sessionJson = JsonSerializer.Serialize(session);
			var options = new DistributedCacheEntryOptions
			{
				AbsoluteExpiration = session.ExpiresAt
			};

			await _cache.SetStringAsync(
				key: $"{_sessionPrefix}{sessionId}",
				value: sessionJson,
				options: options
				);

			return sessionId;
		}

		public async Task<UserSession> GetSessionAsync(string sessionId)
		{
			var sessionJson = await _cache.GetStringAsync(
				key: $"{_sessionPrefix}{sessionId}"
				);

			if (string.IsNullOrEmpty(sessionJson))
				return null;

			return JsonSerializer.Deserialize<UserSession>(sessionJson);
		}

		public async Task<(bool, string)> ValidateSessionAsync(string sessionId)
		{
			var session = await GetSessionAsync(sessionId);
			if (session != null &&
				session.IsActive &&
				session.ExpiresAt > DateTime.UtcNow)
			{
				return (true, session.Username);
			}
			return (false, string.Empty);
		}

		public async Task<bool> DeleteSessionAsync(string sessionId)
		{
			await _cache.RemoveAsync($"{_sessionPrefix}{sessionId}");
			return true;
		}

		private string GenerateSessionId()
		{
			return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
				.Replace("/", "_")
				.Replace("+", "-")
				.Replace("=", "");
		}
	}
}
