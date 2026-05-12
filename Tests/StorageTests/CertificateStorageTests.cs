using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.ViewModels;
using Microsoft.EntityFrameworkCore;
using Models;
using Storage;
using Storage.Models;
using Storage.Storages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.StorageTests
{
	public class CertificateStorageTests
	{
		private static StorageContext CreateContext(string dbName) =>
		new(new DbContextOptionsBuilder<StorageContext>()
		   .UseInMemoryDatabase(dbName)
		   .Options);

		private static async Task<(UserViewModel User, CertificateViewModel Cert)> SeedUserWithCertAsync(
		StorageContext ctx,
		string login = "cert_user@test.ru",
		string number = "CERT-001",
		bool isActual = true)
		{
			// Сначала пользователь без сертификата
			var user = new User
			{
				Fullname = "Владелец сертификата",
				Login = login,
				Email = login,
				RoleId = 1,
				CertificateId = 0,
				SystemRole = SystemRole.Signer,
				Created = DateTime.UtcNow,
				IsActive = true,
			};
			ctx.Users.Add(user);
			await ctx.SaveChangesAsync();

			var cert = new Certificate
			{
				StartDate = DateTime.UtcNow.AddYears(-1),
				FinishDate = DateTime.UtcNow.AddYears(1),
				PublicKey = "PUBLIC_KEY",
				Publisher = "ТестУЦ",
				Owner = "Иван Иванов",
				Number = number,
				UserId = user.Id,
				IsActual = isActual,
				Mode = CertificateMode.Internal,
				FilePath = "/certs/cert.pfx",
			};
			ctx.Certificates.Add(cert);
			await ctx.SaveChangesAsync();

			return (user.GetViewModel, cert.GetViewModel);
		}

		[Fact]
		public async Task CertificateStorage_InsertAsync_ReturnsViewModelAndDeactivatesOld()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_InsertAsync_ReturnsViewModelAndDeactivatesOld));
			var (user, _) = await SeedUserWithCertAsync(ctx, number: "OLD-CERT", isActual: true);
			var storage = new CertificateStorage(ctx);

			var result = await storage.InsertAsync(new CertificateBindingModel
			{
				StartDate = DateTime.UtcNow,
				FinishDate = DateTime.UtcNow.AddYears(1),
				PublicKey = "NEW_PUB_KEY",
				Publisher = "НовыйУЦ",
				Owner = "Иван Иванов",
				Number = "NEW-CERT",
				UserId = user.Id,
				IsActual = true,
				Mode = CertificateMode.Internal,
			});

			Assert.NotNull(result);
			Assert.Equal("NEW-CERT", result.Number);
			Assert.True(result.IsActual);

			// Старый сертификат должен стать неактуальным
			var oldCert = ctx.Certificates.First(c => c.Number == "OLD-CERT");
			Assert.False(oldCert.IsActual);

			// CertificateId пользователя должен обновиться
			var updatedUser = ctx.Users.First(u => u.Id == user.Id);
			Assert.Equal(result.Id, updatedUser.CertificateId);
		}

		[Fact]
		public async Task CertificateStorage_InsertAsync_UserNotFound_ThrowsInvalidOperation()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_InsertAsync_UserNotFound_ThrowsInvalidOperation));
			var storage = new CertificateStorage(ctx);

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				storage.InsertAsync(new CertificateBindingModel
				{
					Number = "CERT-X",
					UserId = 9999, // пользователя нет
					PublicKey = "KEY",
					Publisher = "УЦ",
					Owner = "Кто-то",
				}));
		}

		[Fact]
		public async Task CertificateStorage_InsertAsync_NullModel_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_InsertAsync_NullModel_ReturnsNull));
			var storage = new CertificateStorage(ctx);

			var result = await storage.InsertAsync(null!);

			Assert.Null(result);
		}

		[Fact]
		public async Task CertificateStorage_GetElementAsync_ById_ReturnsCertificate()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_GetElementAsync_ById_ReturnsCertificate));
			var (_, cert) = await SeedUserWithCertAsync(ctx);
			var storage = new CertificateStorage(ctx);

			var result = await storage.GetElementAsync(new CertificateSearchModel { Id = cert.Id });

			Assert.NotNull(result);
			Assert.Equal(cert.Id, result.Id);
		}

		[Fact]
		public async Task CertificateStorage_GetElementAsync_ByNumber_ReturnsCertificate()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_GetElementAsync_ByNumber_ReturnsCertificate));
			await SeedUserWithCertAsync(ctx, number: "FIND-BY-NUM");
			var storage = new CertificateStorage(ctx);

			var result = await storage.GetElementAsync(
				new CertificateSearchModel { Number = "FIND-BY-NUM" });

			Assert.NotNull(result);
			Assert.Equal("FIND-BY-NUM", result.Number);
		}

		[Fact]
		public async Task CertificateStorage_GetElementAsync_ByUserId_ReturnsActualCertificate()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_GetElementAsync_ByUserId_ReturnsActualCertificate));
			var (user, _) = await SeedUserWithCertAsync(ctx, isActual: true);
			var storage = new CertificateStorage(ctx);

			// При поиске по UserId без IsActual — фильтр IsActual=true применяется автоматически
			var result = await storage.GetElementAsync(
				new CertificateSearchModel { UserId = user.Id });

			Assert.NotNull(result);
			Assert.True(result.IsActual);
			Assert.Equal(user.Id, result.UserId);
		}

		[Fact]
		public async Task CertificateStorage_GetElementAsync_ByUserId_InactiveCert_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_GetElementAsync_ByUserId_InactiveCert_ReturnsNull));
			var (user, _) = await SeedUserWithCertAsync(ctx, isActual: false);
			var storage = new CertificateStorage(ctx);

			// Сертификат есть, но неактуален — при поиске по UserId не должен вернуться
			var result = await storage.GetElementAsync(
				new CertificateSearchModel { UserId = user.Id });

			Assert.Null(result);
		}

		[Fact]
		public async Task CertificateStorage_GetElementAsync_EmptySearch_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_GetElementAsync_EmptySearch_ReturnsNull));
			var storage = new CertificateStorage(ctx);

			var result = await storage.GetElementAsync(new CertificateSearchModel());

			Assert.Null(result);
		}

		[Fact]
		public async Task CertificateStorage_GetFilteredList_ByUserId_ReturnsAll()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_GetFilteredList_ByUserId_ReturnsAll));
			var (user, _) = await SeedUserWithCertAsync(ctx, number: "C1", isActual: false);
			// Добавляем второй сертификат тому же пользователю
			ctx.Certificates.Add(new Certificate
			{
				StartDate = DateTime.UtcNow,
				FinishDate = DateTime.UtcNow.AddYears(1),
				PublicKey = "K2",
				Publisher = "УЦ",
				Owner = "Иван",
				Number = "C2",
				UserId = user.Id,
				IsActual = true,
				Mode = CertificateMode.Internal,
			});
			await ctx.SaveChangesAsync();
			var storage = new CertificateStorage(ctx);

			var result = await storage.GetFilteredListAsync(
				new CertificateSearchModel { UserId = user.Id });

			Assert.Equal(2, result.Count);
			Assert.All(result, c => Assert.Equal(user.Id, c.UserId));
		}

		[Fact]
		public async Task CertificateStorage_GetFilteredList_ByIsActual_ReturnsOnlyActual()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_GetFilteredList_ByIsActual_ReturnsOnlyActual));
			await SeedUserWithCertAsync(ctx, "u1@test.ru", "ACT-1", isActual: true);
			await SeedUserWithCertAsync(ctx, "u2@test.ru", "INACT-1", isActual: false);
			var storage = new CertificateStorage(ctx);

			var result = await storage.GetFilteredListAsync(
				new CertificateSearchModel { IsActual = true });

			Assert.Single(result);
			Assert.True(result[0].IsActual);
		}

		[Fact]
		public async Task CertificateStorage_GetFilteredList_ByPublisher_ReturnsMatches()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_GetFilteredList_ByPublisher_ReturnsMatches));
			await SeedUserWithCertAsync(ctx, "u1@test.ru", "C1");
			// Меняем Publisher у первого сертификата
			ctx.Certificates.First().Publisher = "КриптоПро";
			await ctx.SaveChangesAsync();

			await SeedUserWithCertAsync(ctx, "u2@test.ru", "C2");
			ctx.Certificates.OrderBy(c => c.Id).Last().Publisher = "Другой УЦ";
			await ctx.SaveChangesAsync();

			var storage = new CertificateStorage(ctx);

			var result = await storage.GetFilteredListAsync(
				new CertificateSearchModel { Publisher = "КриптоПро" });

			Assert.Single(result);
			Assert.Contains("КриптоПро", result[0].Publisher);
		}

		[Fact]
		public async Task CertificateStorage_GetFilteredList_EmptySearch_ReturnsEmpty()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_GetFilteredList_EmptySearch_ReturnsEmpty));
			await SeedUserWithCertAsync(ctx);
			var storage = new CertificateStorage(ctx);

			var result = await storage.GetFilteredListAsync(new CertificateSearchModel());

			Assert.Empty(result);
		}

		[Fact]
		public async Task CertificateStorage_GetFullList_ReturnsAll()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_GetFullList_ReturnsAll));
			await SeedUserWithCertAsync(ctx, "u1@test.ru", "C1");
			await SeedUserWithCertAsync(ctx, "u2@test.ru", "C2");
			var storage = new CertificateStorage(ctx);

			var result = await storage.GetFullListAsync();

			Assert.Equal(2, result.Count);
		}

		[Fact]
		public async Task CertificateStorage_GetPagedList_ReturnsCorrectPage()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_GetPagedList_ReturnsCorrectPage));
			for (int i = 1; i <= 5; i++)
				await SeedUserWithCertAsync(ctx, $"u{i}@test.ru", $"CERT-{i:000}");
			var storage = new CertificateStorage(ctx);

			var result = await storage.GetPagedListAsync(
				new CertificateSearchModel { PageNumber = 2, PageSize = 2 });

			Assert.Equal(2, result.Count);
		}

		[Fact]
		public async Task CertificateStorage_GetPagedList_InvalidParams_ReturnsEmpty()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_GetPagedList_InvalidParams_ReturnsEmpty));
			await SeedUserWithCertAsync(ctx);
			var storage = new CertificateStorage(ctx);

			var result = await storage.GetPagedListAsync(
				new CertificateSearchModel { PageNumber = 0, PageSize = 10 });

			Assert.Empty(result);
		}

		[Fact]
		public async Task CertificateStorage_UpdateAsync_ChangesFields()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_UpdateAsync_ChangesFields));
			var (_, cert) = await SeedUserWithCertAsync(ctx);
			var storage = new CertificateStorage(ctx);

			var result = await storage.UpdateAsync(new CertificateBindingModel
			{
				Id = cert.Id,
				StartDate = cert.StartDate,
				FinishDate = cert.FinishDate.AddYears(1),
				PublicKey = "UPDATED_KEY",
				Publisher = "НовыйУЦ",
				Owner = cert.Owner,
				Number = cert.Number,
				UserId = cert.UserId,
				IsActual = false,
				Mode = CertificateMode.Local,
			});

			Assert.NotNull(result);
			Assert.Equal("UPDATED_KEY", result.PublicKey);
			Assert.Equal(CertificateMode.Local, result.Mode);
			Assert.False(result.IsActual);
		}

		[Fact]
		public async Task CertificateStorage_UpdateAsync_SetActual_DeactivatesOtherCerts()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_UpdateAsync_SetActual_DeactivatesOtherCerts));
			var (user, cert1) = await SeedUserWithCertAsync(ctx, number: "OLD", isActual: true);
			// Добавляем второй неактуальный сертификат
			ctx.Certificates.Add(new Certificate
			{
				StartDate = DateTime.UtcNow,
				FinishDate = DateTime.UtcNow.AddYears(1),
				PublicKey = "K2",
				Publisher = "УЦ",
				Owner = "Иван",
				Number = "NEW",
				UserId = user.Id,
				IsActual = false,
				Mode = CertificateMode.Internal,
			});
			await ctx.SaveChangesAsync();
			var cert2 = ctx.Certificates.First(c => c.Number == "NEW");
			var storage = new CertificateStorage(ctx);

			// Делаем cert2 актуальным
			await storage.UpdateAsync(new CertificateBindingModel
			{
				Id = cert2.Id,
				StartDate = cert2.StartDate,
				FinishDate = cert2.FinishDate,
				PublicKey = cert2.PublicKey,
				Publisher = cert2.Publisher,
				Owner = cert2.Owner,
				Number = cert2.Number,
				UserId = user.Id,
				IsActual = true,
				Mode = cert2.Mode,
			});

			// Старый сертификат должен стать неактуальным
			var refreshedOld = ctx.Certificates.First(c => c.Number == "OLD");
			Assert.False(refreshedOld.IsActual);

			// CertificateId пользователя должен обновиться
			var refreshedUser = ctx.Users.First(u => u.Id == user.Id);
			Assert.Equal(cert2.Id, refreshedUser.CertificateId);
		}

		[Fact]
		public async Task CertificateStorage_UpdateAsync_NotFound_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_UpdateAsync_NotFound_ReturnsNull));
			var storage = new CertificateStorage(ctx);

			var result = await storage.UpdateAsync(
				new CertificateBindingModel { Id = 9999, Number = "X", UserId = 1 });

			Assert.Null(result);
		}

		[Fact]
		public async Task CertificateStorage_DeleteAsync_RemovesCertificate()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_DeleteAsync_RemovesCertificate));
			var (_, cert) = await SeedUserWithCertAsync(ctx);
			var storage = new CertificateStorage(ctx);

			var deleted = await storage.DeleteAsync(new CertificateBindingModel { Id = cert.Id });
			var afterDelete = await storage.GetElementAsync(new CertificateSearchModel { Id = cert.Id });

			Assert.NotNull(deleted);
			Assert.Equal(cert.Number, deleted.Number);
			Assert.Null(afterDelete); // физически удалён
		}

		[Fact]
		public async Task CertificateStorage_DeleteAsync_NotFound_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(CertificateStorage_DeleteAsync_NotFound_ReturnsNull));
			var storage = new CertificateStorage(ctx);

			var result = await storage.DeleteAsync(new CertificateBindingModel { Id = 9999 });

			Assert.Null(result);
		}
	}
}
