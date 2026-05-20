using Auth;
using Contracts.BindingModels.Authentication;
using Contracts.LogicContracts.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tests.AuthTests
{
	public class CodeVerificationLogicTests
	{
		private static (CodeVerificationLogic logic,
					Mock<IDistributedCache> cacheMock,
					Mock<IEmailService> emailMock)
		BuildCodeVerificationLogic(int codeExpMin = 15)
		{
			var cacheMock = new Mock<IDistributedCache>();
			var emailMock = new Mock<IEmailService>();
			var settings = Options.Create(new RedisSettings { VerificationCodeExpirationMinutes = codeExpMin });
			var authTest = Options.Create(new AuthTestOptions());
			var logic = new CodeVerificationLogic(cacheMock.Object, emailMock.Object, settings, authTest);
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
	}
}
