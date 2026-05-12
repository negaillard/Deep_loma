using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using Logic;
using Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.LogicTests
{
	public class CertificateLogicTests
	{
		private static (
		CertificateLogic logic,
		Mock<ICertificateStorage> certMock,
		Mock<ICertificateGeneratorLogic> genMock)
		BuildCertificateLogic()
		{
			var certMock = new Mock<ICertificateStorage>();
			var genMock = new Mock<ICertificateGeneratorLogic>();
			var logic = new CertificateLogic(certMock.Object, genMock.Object);
			return (logic, certMock, genMock);
		}

		// ── ReadListAsync ──

		[Fact]
		public async Task CertificateLogic_ReadListAsync_NullModel_CallsGetFullList()
		{
			var (logic, certMock, _) = BuildCertificateLogic();
			certMock.Setup(s => s.GetFullListAsync())
					.ReturnsAsync(new List<CertificateViewModel>());

			var result = await logic.ReadListAsync(null);

			certMock.Verify(s => s.GetFullListAsync(), Times.Once);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task CertificateLogic_ReadListAsync_WithModel_CallsGetFilteredList()
		{
			var (logic, certMock, _) = BuildCertificateLogic();
			var model = new CertificateSearchModel { UserId = 1 };
			certMock.Setup(s => s.GetFilteredListAsync(model))
					.ReturnsAsync(new List<CertificateViewModel>
					{
					new() { Id = 1, Number = "CERT-001", UserId = 1 }
					});

			var result = await logic.ReadListAsync(model);

			certMock.Verify(s => s.GetFilteredListAsync(model), Times.Once);
			Assert.Single(result!);
		}

		// ── ReadPagedListAsync ──

		[Fact]
		public async Task CertificateLogic_ReadPagedListAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildCertificateLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.ReadPagedListAsync(null!));
		}

		[Fact]
		public async Task CertificateLogic_ReadPagedListAsync_NoPaginationParams_ThrowsArgumentException()
		{
			var (logic, _, _) = BuildCertificateLogic();

			await Assert.ThrowsAsync<ArgumentException>(() =>
				logic.ReadPagedListAsync(new CertificateSearchModel())); // PageNumber и PageSize не заданы
		}

		[Fact]
		public async Task CertificateLogic_ReadPagedListAsync_ValidParams_ReturnsList()
		{
			var (logic, certMock, _) = BuildCertificateLogic();
			var model = new CertificateSearchModel { PageNumber = 1, PageSize = 10 };
			certMock.Setup(s => s.GetPagedListAsync(model))
					.ReturnsAsync(new List<CertificateViewModel> { new() { Id = 1 } });

			var result = await logic.ReadPagedListAsync(model);

			Assert.Single(result!);
		}

		// ── ReadElementAsync ──

		[Fact]
		public async Task CertificateLogic_ReadElementAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildCertificateLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.ReadElementAsync(null!));
		}

		[Fact]
		public async Task CertificateLogic_ReadElementAsync_NotFound_ReturnsNull()
		{
			var (logic, certMock, _) = BuildCertificateLogic();
			var model = new CertificateSearchModel { Id = 9999 };
			certMock.Setup(s => s.GetElementAsync(model)).ReturnsAsync((CertificateViewModel?)null);

			var result = await logic.ReadElementAsync(model);

			Assert.Null(result);
		}

		[Fact]
		public async Task CertificateLogic_ReadElementAsync_Found_ReturnsViewModel()
		{
			var (logic, certMock, _) = BuildCertificateLogic();
			var model = new CertificateSearchModel { Id = 1 };
			certMock.Setup(s => s.GetElementAsync(model))
					.ReturnsAsync(new CertificateViewModel { Id = 1, Number = "CERT-42" });

			var result = await logic.ReadElementAsync(model);

			Assert.NotNull(result);
			Assert.Equal("CERT-42", result.Number);
		}

		// ── CreateAsync ──

		[Fact]
		public async Task CertificateLogic_CreateAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildCertificateLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.CreateAsync(null!));
		}

		[Fact]
		public async Task CertificateLogic_CreateAsync_EmptyNumber_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildCertificateLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.CreateAsync(new CertificateBindingModel { Number = "", UserId = 1 }));
		}

		[Fact]
		public async Task CertificateLogic_CreateAsync_InvalidUserId_ThrowsArgumentException()
		{
			var (logic, _, _) = BuildCertificateLogic();

			await Assert.ThrowsAsync<ArgumentException>(() =>
				logic.CreateAsync(new CertificateBindingModel { Number = "CERT-X", UserId = 0 }));
		}

		[Fact]
		public async Task CertificateLogic_CreateAsync_DuplicateNumber_ThrowsInvalidOperationException()
		{
			var (logic, certMock, _) = BuildCertificateLogic();
			var model = new CertificateBindingModel { Id = 0, Number = "CERT-DUP", UserId = 1 };

			// В хранилище уже есть сертификат с таким номером (другой Id)
			certMock.Setup(s => s.GetElementAsync(
						It.Is<CertificateSearchModel>(m => m.Number == "CERT-DUP")))
					.ReturnsAsync(new CertificateViewModel { Id = 99, Number = "CERT-DUP" });

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				logic.CreateAsync(model));
		}

		[Fact]
		public async Task CertificateLogic_CreateAsync_Valid_ReturnsTrue()
		{
			var (logic, certMock, _) = BuildCertificateLogic();
			var model = new CertificateBindingModel { Number = "CERT-NEW", UserId = 1 };

			certMock.Setup(s => s.GetElementAsync(It.IsAny<CertificateSearchModel>()))
					.ReturnsAsync((CertificateViewModel?)null); // дубликата нет
			certMock.Setup(s => s.InsertAsync(model))
					.ReturnsAsync(new CertificateViewModel { Id = 1, Number = "CERT-NEW" });

			var result = await logic.CreateAsync(model);

			Assert.True(result);
		}

		[Fact]
		public async Task CertificateLogic_CreateAsync_StorageFails_ReturnsFalse()
		{
			var (logic, certMock, _) = BuildCertificateLogic();
			var model = new CertificateBindingModel { Number = "CERT-FAIL", UserId = 1 };

			certMock.Setup(s => s.GetElementAsync(It.IsAny<CertificateSearchModel>()))
					.ReturnsAsync((CertificateViewModel?)null);
			certMock.Setup(s => s.InsertAsync(model))
					.ReturnsAsync((CertificateViewModel?)null);

			var result = await logic.CreateAsync(model);

			Assert.False(result);
		}

		// ── UpdateAsync ──

		[Fact]
		public async Task CertificateLogic_UpdateAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildCertificateLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.UpdateAsync(null!));
		}

		[Fact]
		public async Task CertificateLogic_UpdateAsync_DuplicateNumber_ThrowsInvalidOperationException()
		{
			var (logic, certMock, _) = BuildCertificateLogic();
			var model = new CertificateBindingModel { Id = 5, Number = "CERT-TAKEN", UserId = 1 };

			// Другая запись (Id=99) с тем же номером
			certMock.Setup(s => s.GetElementAsync(
						It.Is<CertificateSearchModel>(m => m.Number == "CERT-TAKEN")))
					.ReturnsAsync(new CertificateViewModel { Id = 99, Number = "CERT-TAKEN" });

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				logic.UpdateAsync(model));
		}

		[Fact]
		public async Task CertificateLogic_UpdateAsync_Valid_ReturnsTrue()
		{
			var (logic, certMock, _) = BuildCertificateLogic();
			var model = new CertificateBindingModel { Id = 5, Number = "CERT-5", UserId = 1 };

			certMock.Setup(s => s.GetElementAsync(It.IsAny<CertificateSearchModel>()))
					.ReturnsAsync((CertificateViewModel?)null);
			certMock.Setup(s => s.UpdateAsync(model))
					.ReturnsAsync(new CertificateViewModel { Id = 5, Number = "CERT-5" });

			var result = await logic.UpdateAsync(model);

			Assert.True(result);
		}

		[Fact]
		public async Task CertificateLogic_UpdateAsync_StorageFails_ReturnsFalse()
		{
			var (logic, certMock, _) = BuildCertificateLogic();
			var model = new CertificateBindingModel { Id = 5, Number = "CERT-5", UserId = 1 };

			certMock.Setup(s => s.GetElementAsync(It.IsAny<CertificateSearchModel>()))
					.ReturnsAsync((CertificateViewModel?)null);
			certMock.Setup(s => s.UpdateAsync(model))
					.ReturnsAsync((CertificateViewModel?)null);

			var result = await logic.UpdateAsync(model);

			Assert.False(result);
		}

		// ── DeleteAsync ──

		[Fact]
		public async Task CertificateLogic_DeleteAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildCertificateLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.DeleteAsync(null!));
		}

		[Fact]
		public async Task CertificateLogic_DeleteAsync_Valid_ReturnsTrue()
		{
			var (logic, certMock, _) = BuildCertificateLogic();
			var model = new CertificateBindingModel { Id = 7 };

			certMock.Setup(s => s.DeleteAsync(model))
					.ReturnsAsync(new CertificateViewModel { Id = 7 });

			var result = await logic.DeleteAsync(model);

			Assert.True(result);
			certMock.Verify(s => s.DeleteAsync(model), Times.Once);
		}

		[Fact]
		public async Task CertificateLogic_DeleteAsync_StorageFails_ReturnsFalse()
		{
			var (logic, certMock, _) = BuildCertificateLogic();
			var model = new CertificateBindingModel { Id = 7 };

			certMock.Setup(s => s.DeleteAsync(model))
					.ReturnsAsync((CertificateViewModel?)null);

			var result = await logic.DeleteAsync(model);

			Assert.False(result);
		}

		// ── GenerateSelfSignedAsync ──

		[Fact]
		public async Task CertificateLogic_GenerateSelfSigned_CallsGeneratorThenInsert()
		{
			var (logic, certMock, genMock) = BuildCertificateLogic();

			var generatedModel = new CertificateBindingModel
			{
				Number = "GEN-001",
				UserId = 3,
				PublicKey = "PEM_KEY",
				Publisher = "Self",
				Owner = "Иван",
				StartDate = DateTime.UtcNow,
				FinishDate = DateTime.UtcNow.AddYears(1),
				IsActual = true,
			};

			genMock.Setup(g => g.GenerateSelfSignedAsync(3, "Иван", "Self"))
				   .ReturnsAsync(generatedModel);

			// Активных сертификатов нет — деактивировать нечего
			certMock.Setup(s => s.GetFilteredListAsync(
						It.Is<CertificateSearchModel>(m => m.UserId == 3 && m.IsActual == true)))
					.ReturnsAsync(new List<CertificateViewModel>());

			certMock.Setup(s => s.InsertAsync(generatedModel))
					.ReturnsAsync(new CertificateViewModel
					{
						Id = 10,
						Number = "GEN-001",
						UserId = 3,
						IsActual = true
					});

			var result = await logic.GenerateSelfSignedAsync(3, "Иван", "Self");

			Assert.NotNull(result);
			Assert.Equal("GEN-001", result.Number);
			genMock.Verify(g => g.GenerateSelfSignedAsync(3, "Иван", "Self"), Times.Once);
			certMock.Verify(s => s.InsertAsync(generatedModel), Times.Once);
		}

		[Fact]
		public async Task CertificateLogic_GenerateSelfSigned_DeactivatesExistingCertificates()
		{
			var (logic, certMock, genMock) = BuildCertificateLogic();

			var generatedModel = new CertificateBindingModel
			{
				Number = "NEW-GEN",
				UserId = 5,
				PublicKey = "KEY",
				Publisher = "Self",
				Owner = "Петров",
				StartDate = DateTime.UtcNow,
				FinishDate = DateTime.UtcNow.AddYears(1),
				IsActual = true,
			};
			genMock.Setup(g => g.GenerateSelfSignedAsync(5, "Петров", "Self"))
				   .ReturnsAsync(generatedModel);

			// Есть один активный сертификат, который нужно деактивировать
			var existing = new CertificateViewModel
			{
				Id = 20,
				Number = "OLD-CERT",
				UserId = 5,
				IsActual = true,
				PublicKey = "OLD_KEY",
				Publisher = "Old",
				Owner = "Петров",
				StartDate = DateTime.UtcNow.AddYears(-1),
				FinishDate = DateTime.UtcNow,
				Mode = CertificateMode.Internal,
			};
			certMock.Setup(s => s.GetFilteredListAsync(
						It.Is<CertificateSearchModel>(m => m.UserId == 5 && m.IsActual == true)))
					.ReturnsAsync(new List<CertificateViewModel> { existing });

			certMock.Setup(s => s.UpdateAsync(
						It.Is<CertificateBindingModel>(m => m.Id == 20 && m.IsActual == false)))
					.ReturnsAsync(new CertificateViewModel { Id = 20, IsActual = false });

			certMock.Setup(s => s.InsertAsync(generatedModel))
					.ReturnsAsync(new CertificateViewModel { Id = 21, Number = "NEW-GEN", IsActual = true });

			await logic.GenerateSelfSignedAsync(5, "Петров", "Self");

			// Старый сертификат должен быть деактивирован через UpdateAsync
			certMock.Verify(
				s => s.UpdateAsync(It.Is<CertificateBindingModel>(m => m.Id == 20 && !m.IsActual)),
				Times.Once);
		}

		[Fact]
		public async Task CertificateLogic_GenerateSelfSigned_GeneratedModelHasEmptyNumber_ThrowsArgumentNullException()
		{
			var (logic, _, genMock) = BuildCertificateLogic();

			// Генератор вернул модель без номера — CheckModelAsync должен выбросить исключение
			genMock.Setup(g => g.GenerateSelfSignedAsync(1, "Иван", "Self"))
				   .ReturnsAsync(new CertificateBindingModel
				   {
					   Number = "",
					   UserId = 1
				   });

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.GenerateSelfSignedAsync(1, "Иван", "Self"));
		}
	}
}
