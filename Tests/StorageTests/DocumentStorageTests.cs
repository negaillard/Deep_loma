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
	public class DocumentStorageTests
	{
		private static StorageContext CreateContext(string dbName) =>
		new(new DbContextOptionsBuilder<StorageContext>()
		  .UseInMemoryDatabase(dbName)
		  .Options);
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

		private static async Task<DocumentViewModel> SeedDocumentAsync(
	   StorageContext ctx,
	   string title,
	   bool deleted = false,
	   DocumentStatus status = DocumentStatus.NOT_SIGNED)
		{
			var doc = new Document
			{
				Title = title,
				Description = "Описание",
				Path = "/files/test.pdf",
				CreatedAt = DateTime.UtcNow,
				CreatedByUserId = 1,
				Status = status,
				IsDeleted = deleted,
				IsSequential = false,
			};
			ctx.Documents.Add(doc);
			await ctx.SaveChangesAsync();
			return doc.GetViewModel;
		}

		[Fact]
		public async Task DocumentStorage_InsertAsync_ReturnsViewModel()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_InsertAsync_ReturnsViewModel));
			var storage = new DocumentStorage(ctx);

			var result = await storage.InsertAsync(new DocumentBindingModel
			{
				Title = "Договор №1",
				Description = "Описание",
				CreatedAt = DateTime.UtcNow,
				CreatedByUserId = 1,
				Status = DocumentStatus.NOT_SIGNED,
			});

			Assert.NotNull(result);
			Assert.Equal("Договор №1", result.Title);
			Assert.False(result.IsDeleted);
		}

		[Fact]
		public async Task DocumentStorage_InsertAsync_NullModel_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_InsertAsync_NullModel_ReturnsNull));
			var storage = new DocumentStorage(ctx);

			var result = await storage.InsertAsync(null!);

			Assert.Null(result);
		}

		[Fact]
		public async Task DocumentStorage_InsertAsync_WithInactiveUser_ThrowsInvalidOperation()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_InsertAsync_WithInactiveUser_ThrowsInvalidOperation));
			var user = await SeedUserAsync(ctx, "inactive@test.ru", isActive: false);
			var storage = new DocumentStorage(ctx);

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				storage.InsertAsync(new DocumentBindingModel
				{
					Title = "Секретный акт",
					Description = "Описание",
					CreatedAt = DateTime.UtcNow,
					CreatedByUserId = 99,
					UserIds = [user.Id],
				}));
		}

		[Fact]
		public async Task DocumentStorage_InsertAsync_SequentialMode_SetsCorrectOrder()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_InsertAsync_SequentialMode_SetsCorrectOrder));
			var u1 = await SeedUserAsync(ctx, "u1@test.ru");
			var u2 = await SeedUserAsync(ctx, "u2@test.ru");
			var storage = new DocumentStorage(ctx);

			await storage.InsertAsync(new DocumentBindingModel
			{
				Title = "Последовательный",
				Description = "Описание",
				CreatedAt = DateTime.UtcNow,
				CreatedByUserId = 1,
				IsSequential = true,
				UserIds = [u1.Id, u2.Id],
			});

			var docUsers = ctx.DocumentUsers.OrderBy(x => x.Order).ToList();
			Assert.Equal(2, docUsers.Count);
			Assert.Equal(1, docUsers[0].Order);
			Assert.Equal(2, docUsers[1].Order);
		}

		[Fact]
		public async Task DocumentStorage_InsertAsync_ParallelMode_SetsOrderToZero()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_InsertAsync_ParallelMode_SetsOrderToZero));
			var u1 = await SeedUserAsync(ctx, "p1@test.ru");
			var u2 = await SeedUserAsync(ctx, "p2@test.ru");
			var storage = new DocumentStorage(ctx);

			await storage.InsertAsync(new DocumentBindingModel
			{
				Title = "Параллельный",
				Description = "Описание",
				CreatedAt = DateTime.UtcNow,
				CreatedByUserId = 1,
				IsSequential = false,
				UserIds = [u1.Id, u2.Id],
			});

			var docUsers = ctx.DocumentUsers.ToList();
			Assert.All(docUsers, du => Assert.Equal(0, du.Order));
		}

		[Fact]
		public async Task DocumentStorage_GetElementAsync_ById_ReturnsDocument()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_GetElementAsync_ById_ReturnsDocument));
			var seeded = await SeedDocumentAsync(ctx, "Акт приёма");
			var storage = new DocumentStorage(ctx);

			var result = await storage.GetElementAsync(new DocumentSearchModel { Id = seeded.Id });

			Assert.NotNull(result);
			Assert.Equal("Акт приёма", result.Title);
		}

		[Fact]
		public async Task DocumentStorage_GetElementAsync_ByTitle_ReturnsDocument()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_GetElementAsync_ByTitle_ReturnsDocument));
			await SeedDocumentAsync(ctx, "Уникальный заголовок");
			var storage = new DocumentStorage(ctx);

			var result = await storage.GetElementAsync(
				new DocumentSearchModel { Title = "Уникальный заголовок" });

			Assert.NotNull(result);
		}

		[Fact]
		public async Task DocumentStorage_GetElementAsync_EmptySearch_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_GetElementAsync_EmptySearch_ReturnsNull));
			var storage = new DocumentStorage(ctx);

			var result = await storage.GetElementAsync(new DocumentSearchModel());

			Assert.Null(result);
		}

		[Fact]
		public async Task DocumentStorage_GetElementAsync_DeletedDoc_ReturnsNullByDefault()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_GetElementAsync_DeletedDoc_ReturnsNullByDefault));
			var seeded = await SeedDocumentAsync(ctx, "Удалённый", deleted: true);
			var storage = new DocumentStorage(ctx);

			// Без флага IsDeleted=true удалённый документ не должен возвращаться
			var result = await storage.GetElementAsync(new DocumentSearchModel { Id = seeded.Id });

			Assert.Null(result);
		}

		[Fact]
		public async Task DocumentStorage_GetFullList_ExcludesDeleted()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_GetFullList_ExcludesDeleted));
			await SeedDocumentAsync(ctx, "Живой");
			await SeedDocumentAsync(ctx, "Удалённый", deleted: true);
			var storage = new DocumentStorage(ctx);

			var result = await storage.GetFullListAsync();

			Assert.Single(result);
			Assert.Equal("Живой", result[0].Title);
		}

		[Fact]
		public async Task DocumentStorage_GetFilteredList_ByStatus_ReturnsMatches()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_GetFilteredList_ByStatus_ReturnsMatches));
			await SeedDocumentAsync(ctx, "Подписан", status: DocumentStatus.SIGNED);
			await SeedDocumentAsync(ctx, "Не подписан", status: DocumentStatus.NOT_SIGNED);
			var storage = new DocumentStorage(ctx);

			var result = await storage.GetFilteredListAsync(
				new DocumentSearchModel { Status = DocumentStatus.SIGNED });

			Assert.Single(result);
			Assert.Equal("Подписан", result[0].Title);
		}

		[Fact]
		public async Task DocumentStorage_GetFilteredList_ByMultipleStatuses_ReturnsMatches()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_GetFilteredList_ByMultipleStatuses_ReturnsMatches));
			await SeedDocumentAsync(ctx, "Подписан", status: DocumentStatus.SIGNED);
			await SeedDocumentAsync(ctx, "Частично", status: DocumentStatus.PARTLY_SIGNED);
			await SeedDocumentAsync(ctx, "Не подписан", status: DocumentStatus.NOT_SIGNED);
			var storage = new DocumentStorage(ctx);

			var result = await storage.GetFilteredListAsync(new DocumentSearchModel
			{
				Statuses = [DocumentStatus.SIGNED, DocumentStatus.PARTLY_SIGNED],
			});

			Assert.Equal(2, result.Count);
		}

		[Fact]
		public async Task DocumentStorage_GetFilteredList_BySearchText_ReturnsMatches()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_GetFilteredList_BySearchText_ReturnsMatches));
			await SeedDocumentAsync(ctx, "Договор аренды");
			await SeedDocumentAsync(ctx, "Акт выполненных работ");
			var storage = new DocumentStorage(ctx);

			var result = await storage.GetFilteredListAsync(
				new DocumentSearchModel { SearchText = "аренд" });

			Assert.Single(result);
			Assert.Equal("Договор аренды", result[0].Title);
		}

		[Fact]
		public async Task DocumentStorage_GetFilteredPagedList_PaginationWorks()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_GetFilteredPagedList_PaginationWorks));
			for (int i = 1; i <= 7; i++)
				await SeedDocumentAsync(ctx, $"Документ {i}");
			var storage = new DocumentStorage(ctx);

			var (items, total) = await storage.GetFilteredPagedListAsync(
				new DocumentSearchModel { PageNumber = 2, PageSize = 3 });

			Assert.Equal(7, total);
			Assert.Equal(3, items.Count);
		}

		[Fact]
		public async Task DocumentStorage_GetFilteredPagedList_LastPage_ReturnsRemainder()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_GetFilteredPagedList_LastPage_ReturnsRemainder));
			for (int i = 1; i <= 7; i++)
				await SeedDocumentAsync(ctx, $"Д {i}");
			var storage = new DocumentStorage(ctx);

			var (items, total) = await storage.GetFilteredPagedListAsync(
				new DocumentSearchModel { PageNumber = 3, PageSize = 3 });

			Assert.Equal(7, total);
			Assert.Single(items); // 7 % 3 = 1
		}

		[Fact]
		public async Task DocumentStorage_GetFilteredPagedList_InvalidParams_ReturnsEmpty()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_GetFilteredPagedList_InvalidParams_ReturnsEmpty));
			await SeedDocumentAsync(ctx, "Х");
			var storage = new DocumentStorage(ctx);

			var (items, total) = await storage.GetFilteredPagedListAsync(
				new DocumentSearchModel { PageNumber = 0, PageSize = 5 });

			Assert.Equal(0, total);
			Assert.Empty(items);
		}

		[Fact]
		public async Task DocumentStorage_DeleteAsync_SetsIsDeleted()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_DeleteAsync_SetsIsDeleted));
			var seeded = await SeedDocumentAsync(ctx, "К удалению");
			var storage = new DocumentStorage(ctx);

			var result = await storage.DeleteAsync(new DocumentBindingModel { Id = seeded.Id });

			Assert.NotNull(result);
			Assert.True(result.IsDeleted);
		}

		[Fact]
		public async Task DocumentStorage_DeleteAsync_AlreadyDeleted_ReturnsViewModelWithoutDoubleDelete()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_DeleteAsync_AlreadyDeleted_ReturnsViewModelWithoutDoubleDelete));
			var seeded = await SeedDocumentAsync(ctx, "Уже удалён", deleted: true);
			var storage = new DocumentStorage(ctx);

			var result = await storage.DeleteAsync(new DocumentBindingModel { Id = seeded.Id });

			Assert.NotNull(result);
			Assert.True(result.IsDeleted);
		}

		[Fact]
		public async Task DocumentStorage_DeleteAsync_NotFound_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_DeleteAsync_NotFound_ReturnsNull));
			var storage = new DocumentStorage(ctx);

			var result = await storage.DeleteAsync(new DocumentBindingModel { Id = 9999 });

			Assert.Null(result);
		}

		[Fact]
		public async Task DocumentStorage_UpdateAsync_ChangesFields()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_UpdateAsync_ChangesFields));
			var seeded = await SeedDocumentAsync(ctx, "Старый заголовок");
			var storage = new DocumentStorage(ctx);

			var result = await storage.UpdateAsync(new DocumentBindingModel
			{
				Id = seeded.Id,
				Title = "Новый заголовок",
				Description = "Новое описание",
				Path = seeded.Path,
				Status = DocumentStatus.SIGNED,
				IsDeleted = false,
				IsSequential = false,
			});

			Assert.NotNull(result);
			Assert.Equal("Новый заголовок", result.Title);
			Assert.Equal(DocumentStatus.SIGNED, result.Status);
		}

		[Fact]
		public async Task DocumentStorage_UpdateAsync_NotFound_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(DocumentStorage_UpdateAsync_NotFound_ReturnsNull));
			var storage = new DocumentStorage(ctx);

			var result = await storage.UpdateAsync(new DocumentBindingModel { Id = 9999, Title = "Х" });

			Assert.Null(result);
		}
	}
}
