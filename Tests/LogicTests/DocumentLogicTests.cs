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
	public class DocumentLogicTests
	{
		private static (DocumentLogic logic,
					Mock<IDocumentStorage> docMock,
					Mock<IFileStorage> fileMock)
		BuildDocumentLogic()
		{
			var docMock = new Mock<IDocumentStorage>();
			var fileMock = new Mock<IFileStorage>();
			var logic = new DocumentLogic(docMock.Object, fileMock.Object);
			return (logic, docMock, fileMock);
		}

		[Fact]
		public async Task DocumentLogic_ReadListAsync_NullModel_ReturnsFullList()
		{
			var (logic, docMock, _) = BuildDocumentLogic();
			docMock.Setup(s => s.GetFullListAsync())
				   .ReturnsAsync(new List<DocumentViewModel> { new() { Id = 1, Title = "Акт" } });

			var result = await logic.ReadListAsync(null);

			docMock.Verify(s => s.GetFullListAsync(), Times.Once);
			Assert.Single(result!);
		}

		[Fact]
		public async Task DocumentLogic_ReadElementAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildDocumentLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.ReadElementAsync(null!));
		}

		[Fact]
		public async Task DocumentLogic_ReadElementAsync_NotFound_ReturnsNull()
		{
			var (logic, docMock, _) = BuildDocumentLogic();
			var model = new DocumentSearchModel { Id = 9999 };
			docMock.Setup(s => s.GetElementAsync(model)).ReturnsAsync((DocumentViewModel?)null);

			var result = await logic.ReadElementAsync(model);

			Assert.Null(result);
		}

		[Fact]
		public async Task DocumentLogic_CreateAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildDocumentLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.CreateAsync(null!, Stream.Null, ".pdf"));
		}

		[Fact]
		public async Task DocumentLogic_CreateAsync_EmptyTitle_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildDocumentLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.CreateAsync(new DocumentBindingModel { Title = "" }, Stream.Null, ".pdf"));
		}

		[Fact]
		public async Task DocumentLogic_CreateAsync_DuplicateTitle_ThrowsInvalidOperationException()
		{
			var (logic, docMock, _) = BuildDocumentLogic();
			var model = new DocumentBindingModel { Title = "Дубль" };
			docMock.Setup(s => s.GetElementAsync(It.Is<DocumentSearchModel>(m => m.Title == "Дубль")))
				   .ReturnsAsync(new DocumentViewModel { Id = 77, Title = "Дубль" });

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				logic.CreateAsync(model, Stream.Null, ".pdf"));
		}

		[Fact]
		public async Task DocumentLogic_CreateAsync_ValidModel_ReturnsTrue()
		{
			var (logic, docMock, fileMock) = BuildDocumentLogic();
			var model = new DocumentBindingModel
			{
				Title = "Новый документ",
				Description = "Описание",
				CreatedAt = DateTime.UtcNow,
				CreatedByUserId = 1
			};
			docMock.Setup(s => s.GetElementAsync(It.IsAny<DocumentSearchModel>()))
				   .ReturnsAsync((DocumentViewModel?)null);
			docMock.Setup(s => s.InsertAsync(model))
				   .ReturnsAsync(new DocumentViewModel { Id = 10, Title = "Новый документ" });
			fileMock.Setup(f => f.SaveOriginalAsync(10, "Новый документ", It.IsAny<Stream>(), ".pdf"))
					.ReturnsAsync("/files/10.pdf");
			docMock.Setup(s => s.UpdateAsync(It.IsAny<DocumentBindingModel>()))
				   .ReturnsAsync(new DocumentViewModel { Id = 10 });

			var result = await logic.CreateAsync(model, Stream.Null, ".pdf");

			Assert.True(result);
			fileMock.Verify(
				f => f.SaveOriginalAsync(10, "Новый документ", It.IsAny<Stream>(), ".pdf"),
				Times.Once);
		}

		[Fact]
		public async Task DocumentLogic_CreateAsync_StorageInsertFails_ReturnsFalse()
		{
			var (logic, docMock, _) = BuildDocumentLogic();
			var model = new DocumentBindingModel { Title = "Провал" };
			docMock.Setup(s => s.GetElementAsync(It.IsAny<DocumentSearchModel>()))
				   .ReturnsAsync((DocumentViewModel?)null);
			docMock.Setup(s => s.InsertAsync(model))
				   .ReturnsAsync((DocumentViewModel?)null);

			var result = await logic.CreateAsync(model, Stream.Null, ".pdf");

			Assert.False(result);
		}

		[Fact]
		public async Task DocumentLogic_UpdateAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildDocumentLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.UpdateAsync(null!));
		}

		[Fact]
		public async Task DocumentLogic_DeleteAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildDocumentLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.DeleteAsync(null!));
		}

		[Fact]
		public async Task DocumentLogic_DeleteAsync_StorageReturnsNull_ReturnsFalse()
		{
			var (logic, docMock, _) = BuildDocumentLogic();
			var model = new DocumentBindingModel { Id = 5 };
			docMock.Setup(s => s.DeleteAsync(model)).ReturnsAsync((DocumentViewModel?)null);

			var result = await logic.DeleteAsync(model);

			Assert.False(result);
		}

		[Fact]
		public async Task DocumentLogic_DeleteAsync_Valid_ReturnsTrue()
		{
			var (logic, docMock, _) = BuildDocumentLogic();
			var model = new DocumentBindingModel { Id = 5 };
			docMock.Setup(s => s.DeleteAsync(model))
				   .ReturnsAsync(new DocumentViewModel { Id = 5, IsDeleted = true });

			var result = await logic.DeleteAsync(model);

			Assert.True(result);
		}

		[Fact]
		public async Task DocumentLogic_ReadFilteredPagedAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildDocumentLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.ReadFilteredPagedAsync(null!));
		}

		[Fact]
		public async Task DocumentLogic_ReadFilteredPagedAsync_FixesInvalidPageSize()
		{
			var (logic, docMock, _) = BuildDocumentLogic();
			var model = new DocumentSearchModel { PageSize = 200 }; // Превышает максимум 100

			docMock.Setup(s => s.GetFilteredPagedListAsync(It.IsAny<DocumentSearchModel>()))
				   .ReturnsAsync((new List<DocumentViewModel>(), 0));

			var result = await logic.ReadFilteredPagedAsync(model);

			// PageSize должен быть скорректирован до 100
			Assert.Equal(100, result.PageSize);
		}

		[Fact]
		public async Task DocumentLogic_ReadFilteredPagedAsync_FixesInvalidPageNumber()
		{
			var (logic, docMock, _) = BuildDocumentLogic();
			var model = new DocumentSearchModel { PageNumber = 0, PageSize = 10 };

			docMock.Setup(s => s.GetFilteredPagedListAsync(It.IsAny<DocumentSearchModel>()))
				   .ReturnsAsync((new List<DocumentViewModel>(), 5));

			var result = await logic.ReadFilteredPagedAsync(model);

			// PageNumber должен быть скорректирован до 1
			Assert.Equal(1, result.PageNumber);
		}
	}
}
