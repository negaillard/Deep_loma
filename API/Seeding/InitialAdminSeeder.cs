using Contracts;
using Microsoft.EntityFrameworkCore;
using Models.Enums;
using Storage;
using Storage.Models;

namespace API.Seeding
{
	public static class InitialAdminSeeder
	{

		/// <summary>
		/// Запускается при каждом старте приложения:
		/// 1. Гарантирует существование системной роли «Нет роли».
		/// 2. Если пользователей нет — создаёт первоначального администратора.
		/// </summary>
		public static async Task SeedInitialAdminAsync(this WebApplication app)
		{
			var logger = app.Logger;
			var config = app.Configuration;

			using (var scope = app.Services.CreateScope())
			{
				var context = scope.ServiceProvider.GetRequiredService<StorageContext>();

				try
				{
					logger.LogInformation("Применение миграций базы данных при запуске...");
					await context.Database.MigrateAsync();
					logger.LogInformation("Миграции успешно применены.");
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Ошибка при автоматическом применении миграций базы данных.");
				}

				// Всегда гарантируем наличие служебной роли «Нет роли»
				await EnsureNoRoleAsync(context, logger);

				// Далее — только если пользователей ещё нет
				if (await context.Users.AnyAsync())
				{
					logger.LogInformation("Пользователи уже существуют — засев начального администратора пропущен.");
					return;
				}

				var email    = config["InitialAdmin:Email"];
				var login    = config["InitialAdmin:Login"];
				var fullname = config["InitialAdmin:Fullname"];

				if (string.IsNullOrWhiteSpace(email) ||
					string.IsNullOrWhiteSpace(login) ||
					string.IsNullOrWhiteSpace(fullname))
				{
					logger.LogWarning(
						"Секция InitialAdmin не настроена — первоначальный администратор не будет создан. " +
						"Укажите InitialAdmin:Email, InitialAdmin:Login, InitialAdmin:Fullname в конфиге.");
					return;
				}

				logger.LogInformation("База данных пуста. Создание первоначального администратора...");

				var adminRole = await EnsureAdminRoleAsync(context, logger);

				var admin = new User
				{
					Fullname      = fullname,
					Login         = login,
					Email         = email,
					SystemRole    = SystemRole.SystemAdmin,
					RoleId        = adminRole.Id,
					CertificateId = 0,
					Created       = DateTime.UtcNow,
					IsActive      = true,
				};

				await context.Users.AddAsync(admin);
				await context.SaveChangesAsync();

				logger.LogInformation(
					"Первоначальный администратор создан: Login={Login}, Email={Email}",
					admin.Login, admin.Email);
			}
		}

		/// <summary>
		/// Гарантирует наличие служебной роли «Нет роли».
		/// Вызывается при каждом запуске — идемпотентно.
		/// </summary>
		public static async Task<Role> EnsureNoRoleAsync(StorageContext context, ILogger logger)
		{
			var existing = await context.Roles.FirstOrDefaultAsync(r => r.Name == SystemConstants.NoRoleName);
			if (existing != null)
				return existing;

			var role = new Role
			{
				Name        = SystemConstants.NoRoleName,
				Description = "Служебная роль — назначается автоматически при удалении роли пользователя",
			};

			await context.Roles.AddAsync(role);
			await context.SaveChangesAsync();

			logger.LogInformation("Служебная роль '{RoleName}' создана (Id={Id}).", role.Name, role.Id);
			return role;
		}

		private static async Task<Role> EnsureAdminRoleAsync(StorageContext context, ILogger logger)
		{
			const string adminRoleName = "SystemAdmin";

			var existing = await context.Roles.FirstOrDefaultAsync(r => r.Name == adminRoleName);
			if (existing != null)
				return existing;

			var role = new Role
			{
				Name        = adminRoleName,
				Description = "Полный доступ к системе",
			};

			await context.Roles.AddAsync(role);
			await context.SaveChangesAsync();

			logger.LogInformation("Роль '{RoleName}' создана.", adminRoleName);
			return role;
		}
	}
}
