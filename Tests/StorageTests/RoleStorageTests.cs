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

	public class RoleStorageTests
	{
		private static StorageContext CreateContext(string dbName) =>
		new(new DbContextOptionsBuilder<StorageContext>()
			.UseInMemoryDatabase(dbName)
			.Options);

		private static async Task<RoleViewModel> SeedRoleAsync(StorageContext ctx, string name)
		{
			var role = new Role { Name = name, Description = $"Описание: {name}" };
			ctx.Roles.Add(role);
			await ctx.SaveChangesAsync();
			return role.GetViewModel;
		}

		[Fact]
		public async Task RoleStorage_InsertAsync_ReturnsViewModel()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_InsertAsync_ReturnsViewModel));
			var storage = new RoleStorage(ctx);

			var result = await storage.InsertAsync(
				new RoleBindingModel { Name = "Менеджер", Description = "Управляет документами" });

			Assert.NotNull(result);
			Assert.Equal("Менеджер", result.Name);
		}

		[Fact]
		public async Task RoleStorage_InsertAsync_NullModel_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_InsertAsync_NullModel_ReturnsNull));
			var storage = new RoleStorage(ctx);

			var result = await storage.InsertAsync(null!);

			Assert.Null(result);
		}

		[Fact]
		public async Task RoleStorage_GetElementAsync_ByName_ReturnsRole()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_GetElementAsync_ByName_ReturnsRole));
			await SeedRoleAsync(ctx, "Подписант");
			var storage = new RoleStorage(ctx);

			var result = await storage.GetElementAsync(new RoleSearchModel { Name = "Подписант" });

			Assert.NotNull(result);
			Assert.Equal("Подписант", result.Name);
		}

		[Fact]
		public async Task RoleStorage_GetElementAsync_ById_ReturnsRole()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_GetElementAsync_ById_ReturnsRole));
			var seeded = await SeedRoleAsync(ctx, "Администратор");
			var storage = new RoleStorage(ctx);

			var result = await storage.GetElementAsync(new RoleSearchModel { Id = seeded.Id });

			Assert.NotNull(result);
			Assert.Equal(seeded.Id, result.Id);
		}

		[Fact]
		public async Task RoleStorage_GetElementAsync_EmptySearch_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_GetElementAsync_EmptySearch_ReturnsNull));
			var storage = new RoleStorage(ctx);

			var result = await storage.GetElementAsync(new RoleSearchModel());

			Assert.Null(result);
		}

		[Fact]
		public async Task RoleStorage_GetFilteredList_ByNameContains_ReturnsMatches()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_GetFilteredList_ByNameContains_ReturnsMatches));
			await SeedRoleAsync(ctx, "Нет роли");
			await SeedRoleAsync(ctx, "Администратор");
			var storage = new RoleStorage(ctx);

			var result = await storage.GetFilteredListAsync(new RoleSearchModel { Name = "Нет" });

			Assert.Single(result);
			Assert.Equal("Нет роли", result[0].Name);
		}

		[Fact]
		public async Task RoleStorage_GetFilteredList_EmptyName_ReturnsEmpty()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_GetFilteredList_EmptyName_ReturnsEmpty));
			await SeedRoleAsync(ctx, "Любая роль");
			var storage = new RoleStorage(ctx);

			var result = await storage.GetFilteredListAsync(new RoleSearchModel());

			Assert.Empty(result);
		}

		[Fact]
		public async Task RoleStorage_GetFullList_ReturnsAllRoles()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_GetFullList_ReturnsAllRoles));
			await SeedRoleAsync(ctx, "Роль А");
			await SeedRoleAsync(ctx, "Роль Б");
			await SeedRoleAsync(ctx, "Роль В");
			var storage = new RoleStorage(ctx);

			var result = await storage.GetFullListAsync();

			Assert.Equal(3, result.Count);
		}

		[Fact]
		public async Task RoleStorage_GetPagedList_ReturnsCorrectPage()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_GetPagedList_ReturnsCorrectPage));
			for (int i = 1; i <= 5; i++)
				await SeedRoleAsync(ctx, $"Роль {i}");
			var storage = new RoleStorage(ctx);

			var result = await storage.GetPagedListAsync(
				new RoleSearchModel { PageNumber = 1, PageSize = 3 });

			Assert.Equal(3, result.Count);
		}

		[Fact]
		public async Task RoleStorage_GetPagedList_InvalidParams_ReturnsEmpty()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_GetPagedList_InvalidParams_ReturnsEmpty));
			await SeedRoleAsync(ctx, "Роль");
			var storage = new RoleStorage(ctx);

			var result = await storage.GetPagedListAsync(
				new RoleSearchModel { PageNumber = -1, PageSize = 10 });

			Assert.Empty(result);
		}

		[Fact]
		public async Task RoleStorage_UpdateAsync_ChangesName()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_UpdateAsync_ChangesName));
			var seeded = await SeedRoleAsync(ctx, "Старое имя");
			var storage = new RoleStorage(ctx);

			var result = await storage.UpdateAsync(
				new RoleBindingModel { Id = seeded.Id, Name = "Новое имя", Description = "Обновлено" });

			Assert.NotNull(result);
			Assert.Equal("Новое имя", result.Name);
			Assert.Equal("Обновлено", result.Description);
		}

		[Fact]
		public async Task RoleStorage_UpdateAsync_NotFound_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_UpdateAsync_NotFound_ReturnsNull));
			var storage = new RoleStorage(ctx);

			var result = await storage.UpdateAsync(
				new RoleBindingModel { Id = 9999, Name = "Не существует" });

			Assert.Null(result);
		}

		[Fact]
		public async Task RoleStorage_DeleteAsync_RemovesRole()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_DeleteAsync_RemovesRole));
			var seeded = await SeedRoleAsync(ctx, "Временная роль");
			var storage = new RoleStorage(ctx);

			var deleted = await storage.DeleteAsync(new RoleBindingModel { Id = seeded.Id });
			var afterDelete = await storage.GetElementAsync(new RoleSearchModel { Id = seeded.Id });

			Assert.NotNull(deleted);
			Assert.Equal("Временная роль", deleted.Name);
			Assert.Null(afterDelete); // физически удалено
		}

		[Fact]
		public async Task RoleStorage_DeleteAsync_NotFound_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(RoleStorage_DeleteAsync_NotFound_ReturnsNull));
			var storage = new RoleStorage(ctx);

			var result = await storage.DeleteAsync(new RoleBindingModel { Id = 9999 });

			Assert.Null(result);
		}
	}
}
