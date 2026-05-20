using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.ViewModels;
using Microsoft.EntityFrameworkCore;
using Models.Enums;
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
	public class UserStorageTests
	{
		/// <summary>
		/// Создаёт свежий контекст с InMemory-БД.
		/// Имя = имя теста, поэтому каждый тест изолирован.
		/// </summary>
		private static StorageContext CreateContext(string dbName) =>
			new(new DbContextOptionsBuilder<StorageContext>()
				.UseInMemoryDatabase(dbName)
				.Options);

		// seed-хелперы — добавляют сущность в БД и возвращают ViewModel

		private static async Task<UserViewModel> SeedUserAsync(
			StorageContext ctx,
			string login,
			string fullname = "Тестовый Пользователь",
			int roleId = 1,
			bool isActive = true)
		{
			var user = new User
			{
				Fullname = fullname,
				Login = login,
				Email = login,
				RoleId = roleId,
				CertificateId = 1,
				SystemRole = SystemRole.Signer,
				Created = DateTime.UtcNow,
				IsActive = isActive,
			};
			ctx.Users.Add(user);
			await ctx.SaveChangesAsync();
			return user.GetViewModel;
		}

		[Fact]
		public async Task UserStorage_InsertAsync_ReturnsViewModel()
		{
			await using var ctx = CreateContext(nameof(UserStorage_InsertAsync_ReturnsViewModel));
			var storage = new UserStorage(ctx);

			var result = await storage.InsertAsync(new UserBindingModel
			{
				Fullname = "Иван Иванов",
				Login = "ivan@test.ru",
				Email = "ivan@test.ru",
				RoleId = 1,
				CertificateId = 1,
				SystemRole = SystemRole.Signer,
				Created = DateTime.UtcNow,
				IsActive = true,
			});

			Assert.NotNull(result);
			Assert.Equal("Иван Иванов", result.Fullname);
			Assert.Equal("ivan@test.ru", result.Login);
			Assert.True(result.IsActive);
		}

		[Fact]
		public async Task UserStorage_InsertAsync_NullModel_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(UserStorage_InsertAsync_NullModel_ReturnsNull));
			var storage = new UserStorage(ctx);

			var result = await storage.InsertAsync(null!);

			Assert.Null(result);
		}

		[Fact]
		public async Task UserStorage_GetElementAsync_ByLogin_ReturnsUser()
		{
			await using var ctx = CreateContext(nameof(UserStorage_GetElementAsync_ByLogin_ReturnsUser));
			await SeedUserAsync(ctx, "petrov@test.ru");
			var storage = new UserStorage(ctx);

			var result = await storage.GetElementAsync(new UserSearchModel { Login = "petrov@test.ru" });

			Assert.NotNull(result);
			Assert.Equal("petrov@test.ru", result.Login);
		}

		[Fact]
		public async Task UserStorage_GetElementAsync_ById_ReturnsUser()
		{
			await using var ctx = CreateContext(nameof(UserStorage_GetElementAsync_ById_ReturnsUser));
			var seeded = await SeedUserAsync(ctx, "byid@test.ru");
			var storage = new UserStorage(ctx);

			var result = await storage.GetElementAsync(new UserSearchModel { Id = seeded.Id });

			Assert.NotNull(result);
			Assert.Equal(seeded.Id, result.Id);
		}

		[Fact]
		public async Task UserStorage_GetElementAsync_EmptySearch_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(UserStorage_GetElementAsync_EmptySearch_ReturnsNull));
			var storage = new UserStorage(ctx);

			var result = await storage.GetElementAsync(new UserSearchModel());

			Assert.Null(result);
		}

		[Fact]
		public async Task UserStorage_GetElementAsync_FilterByIsActive_ReturnsOnlyActive()
		{
			await using var ctx = CreateContext(nameof(UserStorage_GetElementAsync_FilterByIsActive_ReturnsOnlyActive));
			await SeedUserAsync(ctx, "inactive@test.ru", isActive: false);
			var storage = new UserStorage(ctx);

			// Ищем активного — должен вернуть null, т.к. пользователь неактивен
			var result = await storage.GetElementAsync(
				new UserSearchModel { Login = "inactive@test.ru", IsActive = true });

			Assert.Null(result);
		}

		[Fact]
		public async Task UserStorage_GetFilteredList_ByLogin_ReturnsMatchingUsers()
		{
			await using var ctx = CreateContext(nameof(UserStorage_GetFilteredList_ByLogin_ReturnsMatchingUsers));
			await SeedUserAsync(ctx, "anna@test.ru");
			await SeedUserAsync(ctx, "boris@test.ru");
			var storage = new UserStorage(ctx);

			var result = await storage.GetFilteredListAsync(new UserSearchModel { Login = "anna@test.ru" });

			Assert.Single(result);
			Assert.Equal("anna@test.ru", result[0].Login);
		}

		[Fact]
		public async Task UserStorage_GetFilteredList_ByRoleId_ReturnsMatchingUsers()
		{
			await using var ctx = CreateContext(nameof(UserStorage_GetFilteredList_ByRoleId_ReturnsMatchingUsers));
			await SeedUserAsync(ctx, "role2_a@test.ru", roleId: 2);
			await SeedUserAsync(ctx, "role2_b@test.ru", roleId: 2);
			await SeedUserAsync(ctx, "role3@test.ru", roleId: 3);
			var storage = new UserStorage(ctx);

			var result = await storage.GetFilteredListAsync(new UserSearchModel { RoleId = 2 });

			Assert.Equal(2, result.Count);
			Assert.All(result, u => Assert.Equal(2, u.RoleId));
		}

		[Fact]
		public async Task UserStorage_GetFilteredList_EmptySearch_ReturnsEmpty()
		{
			await using var ctx = CreateContext(nameof(UserStorage_GetFilteredList_EmptySearch_ReturnsEmpty));
			await SeedUserAsync(ctx, "x@test.ru");
			var storage = new UserStorage(ctx);

			var result = await storage.GetFilteredListAsync(new UserSearchModel());

			Assert.Empty(result);
		}

		[Fact]
		public async Task UserStorage_GetFilteredListByFullnameContains_ReturnsMatches()
		{
			await using var ctx = CreateContext(nameof(UserStorage_GetFilteredListByFullnameContains_ReturnsMatches));
			await SeedUserAsync(ctx, "maria@test.ru", fullname: "Мария Петрова");
			await SeedUserAsync(ctx, "ivan@test.ru", fullname: "Иван Сидоров");
			var storage = new UserStorage(ctx);

			var result = await storage.GetFilteredListByFullnameContainsAsync(
				new UserSearchModel { Fullname = "Мария" });

			Assert.Single(result);
			Assert.Contains("Мария", result[0].Fullname);
		}

		[Fact]
		public async Task UserStorage_GetFilteredListByFullnameContains_EmptyFullname_ReturnsEmpty()
		{
			await using var ctx = CreateContext(nameof(UserStorage_GetFilteredListByFullnameContains_EmptyFullname_ReturnsEmpty));
			await SeedUserAsync(ctx, "x@test.ru");
			var storage = new UserStorage(ctx);

			var result = await storage.GetFilteredListByFullnameContainsAsync(
				new UserSearchModel { Fullname = "" });

			Assert.Empty(result);
		}

		[Fact]
		public async Task UserStorage_GetFullList_ReturnsAllUsers()
		{
			await using var ctx = CreateContext(nameof(UserStorage_GetFullList_ReturnsAllUsers));
			await SeedUserAsync(ctx, "a@test.ru");
			await SeedUserAsync(ctx, "b@test.ru");
			var storage = new UserStorage(ctx);

			var result = await storage.GetFullListAsync();

			Assert.Equal(2, result.Count);
		}

		[Fact]
		public async Task UserStorage_GetPagedList_ReturnsCorrectPage()
		{
			await using var ctx = CreateContext(nameof(UserStorage_GetPagedList_ReturnsCorrectPage));
			for (int i = 1; i <= 5; i++)
				await SeedUserAsync(ctx, $"user{i}@test.ru");
			var storage = new UserStorage(ctx);

			var result = await storage.GetPagedListAsync(
				new UserSearchModel { PageNumber = 2, PageSize = 2 });

			Assert.Equal(2, result.Count);
		}

		[Fact]
		public async Task UserStorage_GetPagedList_LastPage_ReturnsRemainder()
		{
			await using var ctx = CreateContext(nameof(UserStorage_GetPagedList_LastPage_ReturnsRemainder));
			for (int i = 1; i <= 5; i++)
				await SeedUserAsync(ctx, $"u{i}@test.ru");
			var storage = new UserStorage(ctx);

			// Страница 3 при размере 2 → 1 запись
			var result = await storage.GetPagedListAsync(
				new UserSearchModel { PageNumber = 3, PageSize = 2 });

			Assert.Single(result);
		}

		[Fact]
		public async Task UserStorage_GetPagedList_InvalidParams_ReturnsEmpty()
		{
			await using var ctx = CreateContext(nameof(UserStorage_GetPagedList_InvalidParams_ReturnsEmpty));
			await SeedUserAsync(ctx, "x@test.ru");
			var storage = new UserStorage(ctx);

			var result = await storage.GetPagedListAsync(
				new UserSearchModel { PageNumber = 0, PageSize = 10 });

			Assert.Empty(result);
		}

		[Fact]
		public async Task UserStorage_UpdateAsync_ChangesFields()
		{
			await using var ctx = CreateContext(nameof(UserStorage_UpdateAsync_ChangesFields));
			var seeded = await SeedUserAsync(ctx, "upd@test.ru");
			var storage = new UserStorage(ctx);

			var result = await storage.UpdateAsync(new UserBindingModel
			{
				Id = seeded.Id,
				Fullname = "Новое Имя",
				Login = "upd@test.ru",
				Email = "upd@test.ru",
				RoleId = 2,
				CertificateId = 2,
				SystemRole = SystemRole.DocumentManager,
				Created = seeded.Created,
				IsActive = false,
			});

			Assert.NotNull(result);
			Assert.Equal("Новое Имя", result.Fullname);
			Assert.False(result.IsActive);
			Assert.Equal(2, result.RoleId);
		}

		[Fact]
		public async Task UserStorage_UpdateAsync_NotFound_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(UserStorage_UpdateAsync_NotFound_ReturnsNull));
			var storage = new UserStorage(ctx);

			var result = await storage.UpdateAsync(new UserBindingModel { Id = 9999, Login = "ghost" });

			Assert.Null(result);
		}

		[Fact]
		public async Task UserStorage_DeleteAsync_SetsIsActiveToFalse()
		{
			await using var ctx = CreateContext(nameof(UserStorage_DeleteAsync_SetsIsActiveToFalse));
			var seeded = await SeedUserAsync(ctx, "del@test.ru");
			var storage = new UserStorage(ctx);

			var result = await storage.DeleteAsync(new UserBindingModel { Id = seeded.Id });

			Assert.NotNull(result);
			Assert.False(result.IsActive); // soft-delete
		}

		[Fact]
		public async Task UserStorage_DeleteAsync_AlreadyInactive_ReturnsViewModelWithoutSaving()
		{
			await using var ctx = CreateContext(nameof(UserStorage_DeleteAsync_AlreadyInactive_ReturnsViewModelWithoutSaving));
			var seeded = await SeedUserAsync(ctx, "already@test.ru", isActive: false);
			var storage = new UserStorage(ctx);

			var result = await storage.DeleteAsync(new UserBindingModel { Id = seeded.Id });

			Assert.NotNull(result);
			Assert.False(result.IsActive);
		}

		[Fact]
		public async Task UserStorage_DeleteAsync_NotFound_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(UserStorage_DeleteAsync_NotFound_ReturnsNull));
			var storage = new UserStorage(ctx);

			var result = await storage.DeleteAsync(new UserBindingModel { Id = 9999 });

			Assert.Null(result);
		}
	}
}
