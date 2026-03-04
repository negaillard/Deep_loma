using Microsoft.EntityFrameworkCore;
using Models;
using Storage;
using Storage.Models;

namespace API.Seeding
{
	public static class InitialAdminSeeder
	{
		/// <summary>
		/// Засевает первоначального системного администратора, если в БД ещё нет ни одного пользователя.
		/// Параметры читаются из секции InitialAdmin конфига или переменных окружения
		/// (InitialAdmin__Email, InitialAdmin__Login, InitialAdmin__Fullname).
		/// Метод идемпотентен: при последующих запусках ничего не делает.
		/// </summary>
		public static async Task SeedInitialAdminAsync(this WebApplication app)
		{
			var logger = app.Logger;
			var config = app.Configuration;

			var email = config["InitialAdmin:Email"];
			var login = config["InitialAdmin:Login"];
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

			await using var context = new StorageContext();

			if (await context.Users.AnyAsync())
			{
				logger.LogInformation("Пользователи уже существуют — засев начального администратора пропущен.");
				return;
			}

			logger.LogInformation("База данных пуста. Создание первоначального администратора...");

			var adminRole = await EnsureAdminRoleAsync(context, logger);

			var admin = new User
			{
				Fullname = fullname,
				Login = login,
				Email = email,
				SystemRole = SystemRole.SystemAdmin,
				RoleId = adminRole.Id,
				CertificateId = 0,
				Created = DateTime.UtcNow,
				IsActive = true,
			};

			await context.Users.AddAsync(admin);
			await context.SaveChangesAsync();

			logger.LogInformation(
				"Первоначальный администратор создан: Login={Login}, Email={Email}",
				admin.Login, admin.Email);
		}

		private static async Task<Role> EnsureAdminRoleAsync(StorageContext context, ILogger logger)
		{
			const string adminRoleName = "SystemAdmin";

			var existing = await context.Roles.FirstOrDefaultAsync(r => r.Name == adminRoleName);
			if (existing != null)
			{
				return existing;
			}

			var role = new Role
			{
				Name = adminRoleName,
				Description = "Полный доступ к системе",
			};

			await context.Roles.AddAsync(role);
			await context.SaveChangesAsync();

			logger.LogInformation("Роль '{RoleName}' создана.", adminRoleName);
			return role;
		}
	}
}
