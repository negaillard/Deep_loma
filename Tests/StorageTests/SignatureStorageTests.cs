using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.ViewModels;
using Microsoft.EntityFrameworkCore;
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
	public class SignatureStorageTests
	{
		private static StorageContext CreateContext(string dbName) =>
		new(new DbContextOptionsBuilder<StorageContext>()
			.UseInMemoryDatabase(dbName)
			.Options);

		private static async Task<SignatureViewModel> SeedSignatureAsync(
		StorageContext ctx,
		string value = "SIG_VALUE",
		int userId = 1,
		int documentId = 1,
		int certificateId = 1,
		bool isDeleted = false)
		{
			var sig = new Signature
			{
				SignatureValue = value,
				Path = "/sigs/test.sig",
				CertificatePath = "/certs/test.cer",
				CerificateId = certificateId,
				SignedAt = DateTime.UtcNow,
				UserId = userId,
				DocumentId = documentId,
				IsDeleted = isDeleted,
			};
			ctx.Signatures.Add(sig);
			await ctx.SaveChangesAsync();
			return sig.GetViewModel;
		}

		[Fact]
		public async Task SignatureStorage_InsertAsync_ReturnsViewModel()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_InsertAsync_ReturnsViewModel));
			var storage = new SignatureStorage(ctx);

			var result = await storage.InsertAsync(new SignatureBindingModel
			{
				SignatureValue = "ABC123",
				CerificateId = 1,
				SignedAt = DateTime.UtcNow,
				UserId = 1,
				DocumentId = 1,
				Path = string.Empty,
				CertificatePath = string.Empty,
			});

			Assert.NotNull(result);
			Assert.Equal("ABC123", result.SignatureValue);
			Assert.False(result.IsDeleted);
		}

		[Fact]
		public async Task SignatureStorage_InsertAsync_NullModel_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_InsertAsync_NullModel_ReturnsNull));
			var storage = new SignatureStorage(ctx);

			var result = await storage.InsertAsync(null!);

			Assert.Null(result);
		}

		[Fact]
		public async Task SignatureStorage_GetElementAsync_ById_ReturnsSignature()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_GetElementAsync_ById_ReturnsSignature));
			var seeded = await SeedSignatureAsync(ctx, "SIG_BY_ID");
			var storage = new SignatureStorage(ctx);

			var result = await storage.GetElementAsync(new SignatureSearchModel { Id = seeded.Id });

			Assert.NotNull(result);
			Assert.Equal(seeded.Id, result.Id);
		}

		[Fact]
		public async Task SignatureStorage_GetElementAsync_BySignatureValue_ReturnsSignature()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_GetElementAsync_BySignatureValue_ReturnsSignature));
			await SeedSignatureAsync(ctx, "UNIQUE_SIG");
			var storage = new SignatureStorage(ctx);

			var result = await storage.GetElementAsync(
				new SignatureSearchModel { SignatureValue = "UNIQUE_SIG" });

			Assert.NotNull(result);
			Assert.Equal("UNIQUE_SIG", result.SignatureValue);
		}

		[Fact]
		public async Task SignatureStorage_GetElementAsync_ByUserAndDocument_ReturnsSignature()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_GetElementAsync_ByUserAndDocument_ReturnsSignature));
			await SeedSignatureAsync(ctx, "SIG_PAIR", userId: 5, documentId: 10);
			var storage = new SignatureStorage(ctx);

			var result = await storage.GetElementAsync(
				new SignatureSearchModel { UserId = 5, DocumentId = 10 });

			Assert.NotNull(result);
			Assert.Equal(5, result.UserId);
			Assert.Equal(10, result.DocumentId);
		}

		[Fact]
		public async Task SignatureStorage_GetElementAsync_EmptySearch_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_GetElementAsync_EmptySearch_ReturnsNull));
			var storage = new SignatureStorage(ctx);

			// Все поля поиска пустые — должен вернуть null без запроса
			var result = await storage.GetElementAsync(new SignatureSearchModel());

			Assert.Null(result);
		}

		[Fact]
		public async Task SignatureStorage_GetElementAsync_DeletedSignature_ReturnsNullByDefault()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_GetElementAsync_DeletedSignature_ReturnsNullByDefault));
			var seeded = await SeedSignatureAsync(ctx, "DELETED_SIG", isDeleted: true);
			var storage = new SignatureStorage(ctx);

			var result = await storage.GetElementAsync(new SignatureSearchModel { Id = seeded.Id });

			Assert.Null(result);
		}

		[Fact]
		public async Task SignatureStorage_GetFilteredList_ByUserId_ReturnsMatches()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_GetFilteredList_ByUserId_ReturnsMatches));
			await SeedSignatureAsync(ctx, "SIG_U1_D1", userId: 1, documentId: 1);
			await SeedSignatureAsync(ctx, "SIG_U1_D2", userId: 1, documentId: 2);
			await SeedSignatureAsync(ctx, "SIG_U2_D1", userId: 2, documentId: 1);
			var storage = new SignatureStorage(ctx);

			var result = await storage.GetFilteredListAsync(new SignatureSearchModel { UserId = 1 });

			Assert.Equal(2, result.Count);
			Assert.All(result, s => Assert.Equal(1, s.UserId));
		}

		[Fact]
		public async Task SignatureStorage_GetFilteredList_ByDocumentId_ReturnsMatches()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_GetFilteredList_ByDocumentId_ReturnsMatches));
			await SeedSignatureAsync(ctx, "SIG_A", userId: 1, documentId: 7);
			await SeedSignatureAsync(ctx, "SIG_B", userId: 2, documentId: 7);
			await SeedSignatureAsync(ctx, "SIG_C", userId: 3, documentId: 8);
			var storage = new SignatureStorage(ctx);

			var result = await storage.GetFilteredListAsync(new SignatureSearchModel { DocumentId = 7 });

			Assert.Equal(2, result.Count);
			Assert.All(result, s => Assert.Equal(7, s.DocumentId));
		}

		[Fact]
		public async Task SignatureStorage_GetFilteredList_ExcludesDeleted()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_GetFilteredList_ExcludesDeleted));
			await SeedSignatureAsync(ctx, "ALIVE", userId: 1, documentId: 1, isDeleted: false);
			await SeedSignatureAsync(ctx, "DELETED", userId: 1, documentId: 2, isDeleted: true);
			var storage = new SignatureStorage(ctx);

			var result = await storage.GetFilteredListAsync(new SignatureSearchModel { UserId = 1 });

			Assert.Single(result);
			Assert.Equal("ALIVE", result[0].SignatureValue);
		}

		[Fact]
		public async Task SignatureStorage_GetFilteredList_EmptySearch_ReturnsEmpty()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_GetFilteredList_EmptySearch_ReturnsEmpty));
			await SeedSignatureAsync(ctx);
			var storage = new SignatureStorage(ctx);

			// Все поля пустые — должен вернуть пустой список
			var result = await storage.GetFilteredListAsync(new SignatureSearchModel());

			Assert.Empty(result);
		}

		[Fact]
		public async Task SignatureStorage_GetFullList_ExcludesDeleted()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_GetFullList_ExcludesDeleted));
			await SeedSignatureAsync(ctx, "LIVE", documentId: 1, isDeleted: false);
			await SeedSignatureAsync(ctx, "REMOVED", documentId: 2, isDeleted: true);
			var storage = new SignatureStorage(ctx);

			var result = await storage.GetFullListAsync();

			Assert.Single(result);
			Assert.Equal("LIVE", result[0].SignatureValue);
		}

		[Fact]
		public async Task SignatureStorage_GetPagedList_ReturnsCorrectPage()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_GetPagedList_ReturnsCorrectPage));
			for (int i = 1; i <= 5; i++)
				await SeedSignatureAsync(ctx, $"SIG_{i}", userId: i, documentId: i);
			var storage = new SignatureStorage(ctx);

			var result = await storage.GetPagedListAsync(
				new SignatureSearchModel { PageNumber = 2, PageSize = 2 });

			Assert.Equal(2, result.Count);
		}

		[Fact]
		public async Task SignatureStorage_GetPagedList_InvalidParams_ReturnsEmpty()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_GetPagedList_InvalidParams_ReturnsEmpty));
			await SeedSignatureAsync(ctx);
			var storage = new SignatureStorage(ctx);

			var result = await storage.GetPagedListAsync(
				new SignatureSearchModel { PageNumber = 0, PageSize = 10 });

			Assert.Empty(result);
		}

		[Fact]
		public async Task SignatureStorage_UpdateAsync_ChangesFields()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_UpdateAsync_ChangesFields));
			var seeded = await SeedSignatureAsync(ctx, "OLD_VALUE");
			var storage = new SignatureStorage(ctx);

			var result = await storage.UpdateAsync(new SignatureBindingModel
			{
				Id = seeded.Id,
				SignatureValue = "NEW_VALUE",
				CerificateId = 2,
				SignedAt = seeded.SignedAt,
				UserId = seeded.UserId,
				DocumentId = seeded.DocumentId,
				Path = "/new/path.sig",
				CertificatePath = "/new/cert.cer",
			});

			Assert.NotNull(result);
			Assert.Equal("NEW_VALUE", result.SignatureValue);
			Assert.Equal("/new/path.sig", result.Path);
			Assert.Equal(2, result.CerificateId);
		}

		[Fact]
		public async Task SignatureStorage_UpdateAsync_NotFound_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_UpdateAsync_NotFound_ReturnsNull));
			var storage = new SignatureStorage(ctx);

			var result = await storage.UpdateAsync(
				new SignatureBindingModel { Id = 9999, SignatureValue = "X" });

			Assert.Null(result);
		}

		[Fact]
		public async Task SignatureStorage_DeleteAsync_SetsIsDeleted()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_DeleteAsync_SetsIsDeleted));
			var seeded = await SeedSignatureAsync(ctx);
			var storage = new SignatureStorage(ctx);

			var result = await storage.DeleteAsync(new SignatureBindingModel { Id = seeded.Id });

			Assert.NotNull(result);
			Assert.True(result.IsDeleted);
		}

		[Fact]
		public async Task SignatureStorage_DeleteAsync_AlreadyDeleted_ReturnsViewModelWithoutDoubleDelete()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_DeleteAsync_AlreadyDeleted_ReturnsViewModelWithoutDoubleDelete));
			var seeded = await SeedSignatureAsync(ctx, isDeleted: true);
			var storage = new SignatureStorage(ctx);

			var result = await storage.DeleteAsync(new SignatureBindingModel { Id = seeded.Id });

			Assert.NotNull(result);
			Assert.True(result.IsDeleted);
		}

		[Fact]
		public async Task SignatureStorage_DeleteAsync_NotFound_ReturnsNull()
		{
			await using var ctx = CreateContext(nameof(SignatureStorage_DeleteAsync_NotFound_ReturnsNull));
			var storage = new SignatureStorage(ctx);

			var result = await storage.DeleteAsync(new SignatureBindingModel { Id = 9999 });

			Assert.Null(result);
		}
	}
}
