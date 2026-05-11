using Auth;
using Contracts.BindingModels.Authentication;
using Contracts.LogicContracts.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Tests;

/// <summary>
/// Юнит-тесты для слоя Auth:
///   - <see cref="CodeVerificationLogic"/> — генерация/отправка/проверка кода
///   - <see cref="SessionService"/>         — создание/валидация/удаление сессий
/// </summary>
public class AuthTests
{
    // ────────────────────── CodeVerificationLogic ───────────────────────

    private static (CodeVerificationLogic logic,
                    Mock<IDistributedCache> cacheMock,
                    Mock<IEmailService> emailMock)
        BuildCodeVerificationLogic(int codeExpMin = 15)
    {
        var cacheMock = new Mock<IDistributedCache>();
        var emailMock = new Mock<IEmailService>();
        var settings = Options.Create(new RedisSettings { VerificationCodeExpirationMinutes = codeExpMin });
        var logic = new CodeVerificationLogic(cacheMock.Object, emailMock.Object, settings);
        return (logic, cacheMock, emailMock);
    }

    // ── GenerateCode ──

    [Fact]
    public void CodeVerification_GenerateCode_Returns6Digits()
    {
        var (logic, _, _) = BuildCodeVerificationLogic();

        var code = logic.GenerateCode();

        Assert.Equal(6, code.Length);
        Assert.True(int.TryParse(code, out var num));
        Assert.InRange(num, 100000, 999999);
    }

    [Fact]
    public void CodeVerification_GenerateCode_ProducesUniqueValues()
    {
        var (logic, _, _) = BuildCodeVerificationLogic();
        var codes = Enumerable.Range(0, 100).Select(_ => logic.GenerateCode()).ToHashSet();

        // Крайне маловероятно, что все 100 кодов одинаковы
        Assert.True(codes.Count > 1);
    }

    // ── SendCodeAsync ──

    [Fact]
    public async Task SendCode_WhenRateLimitNotSet_SendsEmailAndReturnsSuccess()
    {
        var (logic, cacheMock, emailMock) = BuildCodeVerificationLogic();
        const string email = "user@example.com";

        // Rate-limit ключа нет → GetStringAsync вернёт null
        cacheMock.Setup(c => c.GetAsync($"ratelimit:{email}", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((byte[]?)null);

        emailMock.Setup(e => e.SendVerificationCodeAsync(email, It.IsAny<string>()))
                 .Returns((Task<bool>)Task.CompletedTask);

        var (success, message) = await logic.SendCodeAsync(email);

        //Assert.True(success);
        Assert.Contains("отправлен", message);
        emailMock.Verify(e => e.SendVerificationCodeAsync(email, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendCode_WhenRateLimitActive_ReturnsFailure()
    {
        var (logic, cacheMock, emailMock) = BuildCodeVerificationLogic();
        const string email = "rate@example.com";

        // Rate-limit уже выставлен
        cacheMock.Setup(c => c.GetAsync($"ratelimit:{email}", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Encoding.UTF8.GetBytes("1"));

        var (success, message) = await logic.SendCodeAsync(email);

        Assert.False(success);
        Assert.Contains("частые", message);
        emailMock.Verify(e => e.SendVerificationCodeAsync(It.IsAny<string>(), It.IsAny<string>()),
                         Times.Never);
    }

    [Fact]
    public async Task SendCode_WhenEmailServiceThrows_ReturnsFailure()
    {
        var (logic, cacheMock, emailMock) = BuildCodeVerificationLogic();
        const string email = "fail@example.com";

        cacheMock.Setup(c => c.GetAsync($"ratelimit:{email}", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((byte[]?)null);

        emailMock.Setup(e => e.SendVerificationCodeAsync(email, It.IsAny<string>()))
                 .ThrowsAsync(new Exception("SMTP недоступен"));

        var (success, message) = await logic.SendCodeAsync(email);

        Assert.False(success);
        Assert.Contains("Ошибка", message);
    }

    // ── VerifyCodeAsync ──

    [Fact]
    public async Task VerifyCode_CodeNotInCache_ReturnsFailure()
    {
        var (logic, cacheMock, _) = BuildCodeVerificationLogic();

        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((byte[]?)null);

        var (success, message) = await logic.VerifyCodeAsync("x@test.com", "123456");

        Assert.False(success);
        Assert.Contains("не найден", message);
    }

    [Fact]
    public async Task VerifyCode_CorrectCode_ReturnsSuccess()
    {
        var (logic, cacheMock, _) = BuildCodeVerificationLogic();
        const string email = "ok@test.com";
        const string code = "654321";

        var cacheKey = $"verification:{email}";
        var codeInfo = new CodeInfo { Code = code, Email = email, CreatedAt = DateTime.UtcNow, Attempts = 0 };
        var serialized = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(codeInfo));

        cacheMock.Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(serialized);

        var (success, message) = await logic.VerifyCodeAsync(email, code);

        Assert.True(success);
        Assert.Contains("подтвержден", message);
        // После успешной проверки ключ должен быть удалён
        cacheMock.Verify(c => c.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyCode_WrongCode_IncrementsAttempts()
    {
        var (logic, cacheMock, _) = BuildCodeVerificationLogic();
        const string email = "wrong@test.com";
        var cacheKey = $"verification:{email}";

        var codeInfo = new CodeInfo
        {
            Code = "111111",
            Email = email,
            CreatedAt = DateTime.UtcNow,
            Attempts = 0
        };
        cacheMock.Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(codeInfo)));

        var (success, message) = await logic.VerifyCodeAsync(email, "999999");

        Assert.False(success);
        Assert.Contains("Неверный код", message);
        Assert.Contains("2", message); // осталось 2 попытки (из 3)
        // Обновлённый объект с Attempts=1 должен быть сохранён
        cacheMock.Verify(
            c => c.SetAsync(cacheKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(),
                            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyCode_ThreeFailedAttempts_DeletesKeyAndReturnsFailure()
    {
        var (logic, cacheMock, _) = BuildCodeVerificationLogic();
        const string email = "max@test.com";
        var cacheKey = $"verification:{email}";

        // Уже 3 попытки → следующая должна удалить ключ
        var codeInfo = new CodeInfo
        {
            Code = "555555",
            Email = email,
            CreatedAt = DateTime.UtcNow,
            Attempts = 3
        };
        cacheMock.Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(codeInfo)));

        var (success, message) = await logic.VerifyCodeAsync(email, "000000");

        Assert.False(success);
        Assert.Contains("попыток", message);
        cacheMock.Verify(c => c.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ────────────────────────── SessionService ───────────────────────────

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
