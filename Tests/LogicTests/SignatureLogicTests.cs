using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using Logic;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.LogicTests
{
	public class SIgnatureLogicTests
	{
		private static (
		SignatureLogic logic,
		Mock<ISignatureStorage> sigMock,
		Mock<IDocumentStorage> docMock,
		Mock<IFileStorage> fileMock)
		BuildSignatureLogic()
		{
			var sigMock = new Mock<ISignatureStorage>();
			var docMock = new Mock<IDocumentStorage>();
			var fileMock = new Mock<IFileStorage>();
			var logic = new SignatureLogic(sigMock.Object, docMock.Object, fileMock.Object);
			return (logic, sigMock, docMock, fileMock);
		}

		// ── ReadListAsync ──

		[Fact]
		public async Task SignatureLogic_ReadListAsync_NullModel_CallsGetFullList()
		{
			var (logic, sigMock, _, _) = BuildSignatureLogic();
			sigMock.Setup(s => s.GetFullListAsync())
				   .ReturnsAsync(new List<SignatureViewModel>());

			var result = await logic.ReadListAsync(null);

			sigMock.Verify(s => s.GetFullListAsync(), Times.Once);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task SignatureLogic_ReadListAsync_WithModel_CallsGetFilteredList()
		{
			var (logic, sigMock, _, _) = BuildSignatureLogic();
			var model = new SignatureSearchModel { UserId = 1 };
			sigMock.Setup(s => s.GetFilteredListAsync(model))
				   .ReturnsAsync(new List<SignatureViewModel>
				   {
				   new() { Id = 1, UserId = 1, DocumentId = 2, SignatureValue = "SIG" }
				   });

			var result = await logic.ReadListAsync(model);

			sigMock.Verify(s => s.GetFilteredListAsync(model), Times.Once);
			Assert.Single(result!);
		}

		// ── ReadPagedListAsync ──

		[Fact]
		public async Task SignatureLogic_ReadPagedListAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _, _) = BuildSignatureLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.ReadPagedListAsync(null!));
		}

		[Fact]
		public async Task SignatureLogic_ReadPagedListAsync_NoPaginationParams_ThrowsArgumentException()
		{
			var (logic, _, _, _) = BuildSignatureLogic();

			await Assert.ThrowsAsync<ArgumentException>(() =>
				logic.ReadPagedListAsync(new SignatureSearchModel())); // PageNumber и PageSize не заданы
		}

		[Fact]
		public async Task SignatureLogic_ReadPagedListAsync_ValidParams_ReturnsList()
		{
			var (logic, sigMock, _, _) = BuildSignatureLogic();
			var model = new SignatureSearchModel { PageNumber = 1, PageSize = 10 };
			sigMock.Setup(s => s.GetPagedListAsync(model))
				   .ReturnsAsync(new List<SignatureViewModel> { new() { Id = 1 } });

			var result = await logic.ReadPagedListAsync(model);

			Assert.Single(result!);
		}

		// ── ReadElementAsync ──

		[Fact]
		public async Task SignatureLogic_ReadElementAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _, _) = BuildSignatureLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.ReadElementAsync(null!));
		}

		[Fact]
		public async Task SignatureLogic_ReadElementAsync_NotFound_ReturnsNull()
		{
			var (logic, sigMock, _, _) = BuildSignatureLogic();
			var model = new SignatureSearchModel { Id = 9999 };
			sigMock.Setup(s => s.GetElementAsync(model)).ReturnsAsync((SignatureViewModel?)null);

			var result = await logic.ReadElementAsync(model);

			Assert.Null(result);
		}

		[Fact]
		public async Task SignatureLogic_ReadElementAsync_Found_ReturnsViewModel()
		{
			var (logic, sigMock, _, _) = BuildSignatureLogic();
			var model = new SignatureSearchModel { Id = 1 };
			sigMock.Setup(s => s.GetElementAsync(model))
				   .ReturnsAsync(new SignatureViewModel { Id = 1, SignatureValue = "SIG" });

			var result = await logic.ReadElementAsync(model);

			Assert.NotNull(result);
			Assert.Equal("SIG", result.SignatureValue);
		}

		// ── CreateAsync ──

		[Fact]
		public async Task SignatureLogic_CreateAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _, _) = BuildSignatureLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.CreateAsync(null!, Stream.Null));
		}

		[Fact]
		public async Task SignatureLogic_CreateAsync_EmptySignatureValue_ThrowsArgumentNullException()
		{
			var (logic, _, _, _) = BuildSignatureLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.CreateAsync(
					new SignatureBindingModel { SignatureValue = "", UserId = 1, DocumentId = 1 },
					Stream.Null));
		}

		[Fact]
		public async Task SignatureLogic_CreateAsync_InvalidUserId_ThrowsArgumentException()
		{
			var (logic, _, _, _) = BuildSignatureLogic();

			await Assert.ThrowsAsync<ArgumentException>(() =>
				logic.CreateAsync(
					new SignatureBindingModel { SignatureValue = "SIG", UserId = 0, DocumentId = 1 },
					Stream.Null));
		}

		[Fact]
		public async Task SignatureLogic_CreateAsync_InvalidDocumentId_ThrowsArgumentException()
		{
			var (logic, _, _, _) = BuildSignatureLogic();

			await Assert.ThrowsAsync<ArgumentException>(() =>
				logic.CreateAsync(
					new SignatureBindingModel { SignatureValue = "SIG", UserId = 1, DocumentId = 0 },
					Stream.Null));
		}

		[Fact]
		public async Task SignatureLogic_CreateAsync_DuplicateSignature_ThrowsInvalidOperationException()
		{
			var (logic, sigMock, _, _) = BuildSignatureLogic();
			var model = new SignatureBindingModel
			{
				Id = 0,
				SignatureValue = "SIG",
				UserId = 1,
				DocumentId = 1
			};
			// В хранилище уже есть подпись от этого же пользователя на этот же документ
			sigMock.Setup(s => s.GetElementAsync(
						It.Is<SignatureSearchModel>(m => m.UserId == 1 && m.DocumentId == 1)))
				   .ReturnsAsync(new SignatureViewModel { Id = 99, UserId = 1, DocumentId = 1 });

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				logic.CreateAsync(model, Stream.Null));
		}

		[Fact]
		public async Task SignatureLogic_CreateAsync_Valid_SavesFileAndReturnsTrue()
		{
			var (logic, sigMock, docMock, fileMock) = BuildSignatureLogic();
			var model = new SignatureBindingModel
			{
				Id = 0,
				SignatureValue = "NEW_SIG",
				UserId = 3,
				DocumentId = 7
			};

			// Дубликата нет
			sigMock.Setup(s => s.GetElementAsync(
						It.Is<SignatureSearchModel>(m => m.UserId == 3 && m.DocumentId == 7)))
				   .ReturnsAsync((SignatureViewModel?)null);

			// Вставка создаёт запись с Id=10
			sigMock.Setup(s => s.InsertAsync(model))
				   .ReturnsAsync(new SignatureViewModel { Id = 10, UserId = 3, DocumentId = 7 });

			// Документ существует
			docMock.Setup(d => d.GetElementAsync(
						It.Is<DocumentSearchModel>(m => m.Id == 7)))
				   .ReturnsAsync(new DocumentViewModel { Id = 7, Title = "Договор" });

			// Файловое хранилище возвращает путь
			fileMock.Setup(f => f.SaveSignatureAsync(7, "Договор", 3, It.IsAny<Stream>()))
					.ReturnsAsync("/sigs/7_3.sig");

			// После обновления пути
			sigMock.Setup(s => s.UpdateAsync(It.Is<SignatureBindingModel>(m => m.Id == 10)))
				   .ReturnsAsync(new SignatureViewModel { Id = 10, Path = "/sigs/7_3.sig" });

			var result = await logic.CreateAsync(model, Stream.Null);

			Assert.True(result);
			fileMock.Verify(f => f.SaveSignatureAsync(7, "Договор", 3, It.IsAny<Stream>()), Times.Once);
			sigMock.Verify(s => s.UpdateAsync(It.Is<SignatureBindingModel>(m => m.Id == 10)), Times.Once);
		}

		[Fact]
		public async Task SignatureLogic_CreateAsync_DocumentNotFound_ThrowsInvalidOperation()
		{
			var (logic, sigMock, docMock, _) = BuildSignatureLogic();
			var model = new SignatureBindingModel
			{
				Id = 0,
				SignatureValue = "SIG",
				UserId = 1,
				DocumentId = 99
			};

			sigMock.Setup(s => s.GetElementAsync(It.IsAny<SignatureSearchModel>()))
				   .ReturnsAsync((SignatureViewModel?)null);
			sigMock.Setup(s => s.InsertAsync(model))
				   .ReturnsAsync(new SignatureViewModel { Id = 5, UserId = 1, DocumentId = 99 });
			docMock.Setup(d => d.GetElementAsync(It.Is<DocumentSearchModel>(m => m.Id == 99)))
				   .ReturnsAsync((DocumentViewModel?)null);

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				logic.CreateAsync(model, Stream.Null));
		}

		[Fact]
		public async Task SignatureLogic_CreateAsync_StorageInsertFails_ReturnsFalse()
		{
			var (logic, sigMock, _, _) = BuildSignatureLogic();
			var model = new SignatureBindingModel
			{
				Id = 0,
				SignatureValue = "SIG",
				UserId = 1,
				DocumentId = 1
			};

			sigMock.Setup(s => s.GetElementAsync(It.IsAny<SignatureSearchModel>()))
				   .ReturnsAsync((SignatureViewModel?)null);
			sigMock.Setup(s => s.InsertAsync(model))
				   .ReturnsAsync((SignatureViewModel?)null);

			var result = await logic.CreateAsync(model, Stream.Null);

			Assert.False(result);
		}

		// ── UpdateAsync ──

		[Fact]
		public async Task SignatureLogic_UpdateAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _, _) = BuildSignatureLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.UpdateAsync(null!));
		}

		[Fact]
		public async Task SignatureLogic_UpdateAsync_DuplicateForSameUserDoc_ThrowsInvalidOperation()
		{
			var (logic, sigMock, _, _) = BuildSignatureLogic();
			var model = new SignatureBindingModel
			{
				Id = 1,
				SignatureValue = "SIG",
				UserId = 2,
				DocumentId = 5
			};
			// Другая запись (Id != 1) с тем же userId+documentId
			sigMock.Setup(s => s.GetElementAsync(
						It.Is<SignatureSearchModel>(m => m.UserId == 2 && m.DocumentId == 5)))
				   .ReturnsAsync(new SignatureViewModel { Id = 77, UserId = 2, DocumentId = 5 });

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				logic.UpdateAsync(model));
		}

		[Fact]
		public async Task SignatureLogic_UpdateAsync_Valid_ReturnsTrue()
		{
			var (logic, sigMock, _, _) = BuildSignatureLogic();
			var model = new SignatureBindingModel
			{
				Id = 5,
				SignatureValue = "UPDATED",
				UserId = 1,
				DocumentId = 3
			};

			sigMock.Setup(s => s.GetElementAsync(It.IsAny<SignatureSearchModel>()))
				   .ReturnsAsync((SignatureViewModel?)null); // дубликата нет
			sigMock.Setup(s => s.UpdateAsync(model))
				   .ReturnsAsync(new SignatureViewModel { Id = 5, SignatureValue = "UPDATED" });

			var result = await logic.UpdateAsync(model);

			Assert.True(result);
		}

		[Fact]
		public async Task SignatureLogic_UpdateAsync_StorageFails_ReturnsFalse()
		{
			var (logic, sigMock, _, _) = BuildSignatureLogic();
			var model = new SignatureBindingModel
			{
				Id = 5,
				SignatureValue = "UPDATED",
				UserId = 1,
				DocumentId = 3
			};

			sigMock.Setup(s => s.GetElementAsync(It.IsAny<SignatureSearchModel>()))
				   .ReturnsAsync((SignatureViewModel?)null);
			sigMock.Setup(s => s.UpdateAsync(model))
				   .ReturnsAsync((SignatureViewModel?)null);

			var result = await logic.UpdateAsync(model);

			Assert.False(result);
		}

		// ── DeleteAsync ──

		[Fact]
		public async Task SignatureLogic_DeleteAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _, _) = BuildSignatureLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.DeleteAsync(null!));
		}

		[Fact]
		public async Task SignatureLogic_DeleteAsync_Valid_ReturnsTrue()
		{
			var (logic, sigMock, _, _) = BuildSignatureLogic();
			var model = new SignatureBindingModel { Id = 3 };

			sigMock.Setup(s => s.DeleteAsync(model))
				   .ReturnsAsync(new SignatureViewModel { Id = 3, IsDeleted = true });

			var result = await logic.DeleteAsync(model);

			Assert.True(result);
			sigMock.Verify(s => s.DeleteAsync(model), Times.Once);
		}

		[Fact]
		public async Task SignatureLogic_DeleteAsync_StorageFails_ReturnsFalse()
		{
			var (logic, sigMock, _, _) = BuildSignatureLogic();
			var model = new SignatureBindingModel { Id = 3 };

			sigMock.Setup(s => s.DeleteAsync(model))
				   .ReturnsAsync((SignatureViewModel?)null);

			var result = await logic.DeleteAsync(model);

			Assert.False(result);
		}
	}
}
