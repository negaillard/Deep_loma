using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Contracts.BindingModels.Authentication;
using Contracts.LogicContracts.Authentication;

namespace Logic.Authentication
{
	public class CodeVerificationLogic : ICodeVerificationLogic
	{
		private readonly IDistributedCache _cache;
		private readonly IEmailService _emailService;
		private readonly RedisSettings _settings;

		public CodeVerificationLogic(
			IDistributedCache cache,
			IEmailService emailService,
			IOptions<RedisSettings> settings)
		{
			_cache = cache;
			_emailService = emailService;
			_settings = settings.Value;
		}

		public string GenerateCode()
		{
			var random = new Random();
			return random.Next(100000, 999999).ToString();
		}

		public async Task<(bool success, string message)> SendCodeAsync(string email)
		{
			try
			{
				// Проверяем rate limiting (не чаще чем раз в 1 минуту)
				var rateLimitKey = $"ratelimit:{email}";
				var existingRateLimit = await _cache.GetStringAsync(rateLimitKey);
				if (existingRateLimit != null)
				{
					return (false, "Слишком частые запросы. Попробуйте через 1 минуту.");
				}

				// Устанавливаем rate limit на 1 минуту
				// Как будут выглядеть ключи в redis с использованием InstanceName = "AuthServer_"
				// Это нужно, что 
				// AuthServer_verification:EmailVerification:user@example.com
				// AuthServer_ratelimit:user@example.com

				await _cache.SetStringAsync(
					key: rateLimitKey,
					value: "1",
					options: new DistributedCacheEntryOptions
					{
						AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
					});

				// Генерируем код
				var code = GenerateCode();
				var codeInfo = new CodeInfo
				{
					Code = code,
					Email = email,
					CreatedAt = DateTime.UtcNow,
					Attempts = 0 // счетчик попыток ввода
				};

				// Сохраняем в Redis
				var cacheKey = GetCacheKey(email);
				var serializedCodeInfo = JsonSerializer.Serialize(codeInfo);

				await _cache.SetStringAsync(
					key: cacheKey,
					value: serializedCodeInfo,
					options: new DistributedCacheEntryOptions
					{
						AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_settings.VerificationCodeExpirationMinutes)
					});

				// Отправляем email
				await _emailService.SendVerificationCodeAsync(email, code);

				return (true, "Код отправлен на email");
			}
			catch (Exception ex)
			{
				return (false, $"Ошибка отправки: {ex.Message}");
			}
		}

		public async Task<(bool success, string message)> VerifyCodeAsync(string email, string code)
		{
			try
			{
				var cacheKey = GetCacheKey(email);
				var serializedCodeInfo = await _cache.GetStringAsync(cacheKey);

				if (string.IsNullOrEmpty(serializedCodeInfo))
					return (false, "Код не найден или устарел. Запросите новый код.");

				var codeInfo = JsonSerializer.Deserialize<CodeInfo>(serializedCodeInfo);

				// Проверяем количество попыток
				if (codeInfo.Attempts >= 3)
				{
					await _cache.RemoveAsync(cacheKey); // Удаляем код после 3 неудачных попыток
					return (false, "Слишком много неверных попыток. Запросите новый код.");
				}

				// Проверяем код
				if (codeInfo.Code != code)
				{
					// Увеличиваем счетчик попыток
					codeInfo.Attempts++;
					var updatedSerializedCodeInfo = JsonSerializer.Serialize(codeInfo);
					await _cache.SetStringAsync(
						key: cacheKey,
						value: updatedSerializedCodeInfo,
						options: new DistributedCacheEntryOptions
						{
							AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_settings.VerificationCodeExpirationMinutes)
						});

					var attemptsLeft = 3 - codeInfo.Attempts;
					return (false, $"Неверный код. Осталось попыток: {attemptsLeft}");
				}

				// Код верный - удаляем из Redis
				await _cache.RemoveAsync(cacheKey);
				return (true, "Код подтвержден");
			}
			catch (Exception ex)
			{
				return (false, $"Ошибка проверки кода: {ex.Message}");
			}
		}

		private static string GetCacheKey(string email)
		{
			// verification:Login:anasirov@gmail.com
			return $"verification:{email.ToLowerInvariant()}";
		}
	}

	public class RedisSettings
	{
		public int VerificationCodeExpirationMinutes { get; set; } = 15;
	}
}
