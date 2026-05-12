using Auth;
using Contracts.BindingModels.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tests.AuthTests
{
	public class SesssionServiceTests
	{
		private static (SessionService service, Mock<IDistributedCache> cacheMock)
		BuildSessionService()
		{
			var cacheMock = new Mock<IDistributedCache>();
			var service = new SessionService(cacheMock.Object);
			return (service, cacheMock);
		}

		// ── CreateSessionAsync ──

		[Fact]
		public async Task CreateSession_ReturnsNonEmptySessionId()
		{
			var (service, cacheMock) = BuildSessionService();

			var sessionId = await service.CreateSessionAsync(1, "ivan");

			Assert.False(string.IsNullOrWhiteSpace(sessionId));
			// Убеждаемся, что сессия была сохранена в кэш
			cacheMock.Verify(
				c => c.SetAsync(
					It.Is<string>(k => k.StartsWith("session:")),
					It.IsAny<byte[]>(),
					It.IsAny<DistributedCacheEntryOptions>(),
					It.IsAny<CancellationToken>()),
				Times.Once);
		}

		[Fact]
		public async Task CreateSession_TwoCalls_ReturnDifferentIds()
		{
			var (service, _) = BuildSessionService();

			var id1 = await service.CreateSessionAsync(1, "user1");
			var id2 = await service.CreateSessionAsync(2, "user2");

			Assert.NotEqual(id1, id2);
		}

		// ── GetSessionAsync ──

		[Fact]
		public async Task GetSession_NotInCache_ReturnsNull()
		{
			var (service, cacheMock) = BuildSessionService();
			cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
					 .ReturnsAsync((byte[]?)null);

			var result = await service.GetSessionAsync("no-such-session");

			Assert.Null(result);
		}

		[Fact]
		public async Task GetSession_InCache_ReturnsDeserializedSession()
		{
			var (service, cacheMock) = BuildSessionService();
			const string sessionId = "my-session";
			var session = new UserSession
			{
				SessionId = sessionId,
				UserId = 42,
				Username = "petrov",
				CreatedAt = DateTime.UtcNow,
				ExpiresAt = DateTime.UtcNow.AddHours(24),
				IsActive = true
			};
			var json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(session));

			cacheMock.Setup(c => c.GetAsync($"session:{sessionId}", It.IsAny<CancellationToken>()))
					 .ReturnsAsync(json);

			var result = await service.GetSessionAsync(sessionId);

			Assert.NotNull(result);
			Assert.Equal(42, result.UserId);
			Assert.Equal("petrov", result.Username);
		}

		// ── ValidateSessionAsync ──

		[Fact]
		public async Task ValidateSession_ActiveNotExpired_ReturnsTrue()
		{
			var (service, cacheMock) = BuildSessionService();
			const string sessionId = "valid-session";
			var session = new UserSession
			{
				SessionId = sessionId,
				UserId = 1,
				Username = "alice",
				CreatedAt = DateTime.UtcNow.AddMinutes(-5),
				ExpiresAt = DateTime.UtcNow.AddHours(23), // не истекла
				IsActive = true
			};
			cacheMock.Setup(c => c.GetAsync($"session:{sessionId}", It.IsAny<CancellationToken>()))
					 .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(session)));

			var (valid, username) = await service.ValidateSessionAsync(sessionId);

			Assert.True(valid);
			Assert.Equal("alice", username);
		}

		[Fact]
		public async Task ValidateSession_ExpiredSession_ReturnsFalse()
		{
			var (service, cacheMock) = BuildSessionService();
			const string sessionId = "expired-session";
			var session = new UserSession
			{
				SessionId = sessionId,
				UserId = 1,
				Username = "bob",
				CreatedAt = DateTime.UtcNow.AddHours(-48),
				ExpiresAt = DateTime.UtcNow.AddHours(-24), // уже истекла
				IsActive = true
			};
			cacheMock.Setup(c => c.GetAsync($"session:{sessionId}", It.IsAny<CancellationToken>()))
					 .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(session)));

			var (valid, username) = await service.ValidateSessionAsync(sessionId);

			Assert.False(valid);
			Assert.Equal(string.Empty, username);
		}

		[Fact]
		public async Task ValidateSession_InactiveSession_ReturnsFalse()
		{
			var (service, cacheMock) = BuildSessionService();
			const string sessionId = "inactive-session";
			var session = new UserSession
			{
				SessionId = sessionId,
				UserId = 1,
				Username = "carol",
				CreatedAt = DateTime.UtcNow,
				ExpiresAt = DateTime.UtcNow.AddHours(24),
				IsActive = false // явно деактивирована
			};
			cacheMock.Setup(c => c.GetAsync($"session:{sessionId}", It.IsAny<CancellationToken>()))
					 .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(session)));

			var (valid, _) = await service.ValidateSessionAsync(sessionId);

			Assert.False(valid);
		}

		[Fact]
		public async Task ValidateSession_NotInCache_ReturnsFalse()
		{
			var (service, cacheMock) = BuildSessionService();
			cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
					 .ReturnsAsync((byte[]?)null);

			var (valid, username) = await service.ValidateSessionAsync("ghost");

			Assert.False(valid);
			Assert.Equal(string.Empty, username);
		}

		// ── DeleteSessionAsync ──

		[Fact]
		public async Task DeleteSession_CallsRemoveAndReturnsTrue()
		{
			var (service, cacheMock) = BuildSessionService();
			const string sessionId = "to-delete";

			var result = await service.DeleteSessionAsync(sessionId);

			Assert.True(result);
			cacheMock.Verify(
				c => c.RemoveAsync($"session:{sessionId}", It.IsAny<CancellationToken>()),
				Times.Once);
		}
	}
}
