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
	public class UserLogicTests
	{
		private static (UserLogic logic, Mock<IUserStorage> storageMock, Mock<IDocumentUserStorage> dusMock)
		BuildUserLogic()
		{
			var storageMock = new Mock<IUserStorage>();
			var dusMock = new Mock<IDocumentUserStorage>();
			var logic = new UserLogic(storageMock.Object, dusMock.Object);
			return (logic, storageMock, dusMock);
		}

		[Fact]
		public async Task UserLogic_ReadListAsync_NullModel_CallsGetFullList()
		{
			var (logic, storage, _) = BuildUserLogic();
			storage.Setup(s => s.GetFullListAsync())
				   .ReturnsAsync(new List<UserViewModel>());

			var result = await logic.ReadListAsync(null);

			storage.Verify(s => s.GetFullListAsync(), Times.Once);
			Assert.NotNull(result);
		}

		[Fact]
		public async Task UserLogic_ReadListAsync_WithModel_CallsGetFilteredList()
		{
			var (logic, storage, _) = BuildUserLogic();
			var model = new UserSearchModel { Login = "test@test.ru" };
			storage.Setup(s => s.GetFilteredListAsync(model))
				   .ReturnsAsync(new List<UserViewModel>
				   {
				   new() { Id = 1, Login = "test@test.ru", IsActive = true }
				   });

			var result = await logic.ReadListAsync(model);

			storage.Verify(s => s.GetFilteredListAsync(model), Times.Once);
			Assert.Single(result!);
		}

		[Fact]
		public async Task UserLogic_ReadPagedListAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildUserLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.ReadPagedListAsync(null!));
		}

		[Fact]
		public async Task UserLogic_ReadPagedListAsync_NoPaginationParams_ThrowsArgumentException()
		{
			var (logic, _, _) = BuildUserLogic();

			await Assert.ThrowsAsync<ArgumentException>(() =>
				logic.ReadPagedListAsync(new UserSearchModel()));
		}

		[Fact]
		public async Task UserLogic_ReadPagedListAsync_ValidParams_ReturnsPage()
		{
			var (logic, storage, _) = BuildUserLogic();
			var model = new UserSearchModel { PageNumber = 1, PageSize = 10 };
			storage.Setup(s => s.GetPagedListAsync(model))
				   .ReturnsAsync(new List<UserViewModel> { new() { Id = 1 } });

			var result = await logic.ReadPagedListAsync(model);

			Assert.Single(result!);
		}

		[Fact]
		public async Task UserLogic_ReadElementAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildUserLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.ReadElementAsync(null!));
		}

		[Fact]
		public async Task UserLogic_ReadElementAsync_NotFound_ReturnsNull()
		{
			var (logic, storage, _) = BuildUserLogic();
			var model = new UserSearchModel { Login = "nobody@test.ru" };
			storage.Setup(s => s.GetElementAsync(model)).ReturnsAsync((UserViewModel?)null);

			var result = await logic.ReadElementAsync(model);

			Assert.Null(result);
		}

		[Fact]
		public async Task UserLogic_CreateAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildUserLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.CreateAsync(null!));
		}

		[Fact]
		public async Task UserLogic_CreateAsync_EmptyLogin_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildUserLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.CreateAsync(new UserBindingModel { Login = "" }));
		}

		[Fact]
		public async Task UserLogic_CreateAsync_DuplicateLogin_ThrowsInvalidOperationException()
		{
			var (logic, storage, _) = BuildUserLogic();
			var model = new UserBindingModel
			{
				Id = 0,
				Login = "dup@test.ru",
				Fullname = "Дубликат"
			};
			// В хранилище уже есть пользователь с этим логином (другой Id)
			storage.Setup(s => s.GetElementAsync(It.Is<UserSearchModel>(m => m.Login == "dup@test.ru")))
				   .ReturnsAsync(new UserViewModel { Id = 99, Login = "dup@test.ru" });

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				logic.CreateAsync(model));
		}

		[Fact]
		public async Task UserLogic_CreateAsync_Valid_ReturnsTrue()
		{
			var (logic, storage, _) = BuildUserLogic();
			var model = new UserBindingModel
			{
				Id = 0,
				Login = "new@test.ru",
				Fullname = "Новый"
			};
			storage.Setup(s => s.GetElementAsync(It.IsAny<UserSearchModel>()))
				   .ReturnsAsync((UserViewModel?)null);
			storage.Setup(s => s.InsertAsync(model))
				   .ReturnsAsync(new UserViewModel { Id = 1, Login = "new@test.ru" });

			var result = await logic.CreateAsync(model);

			Assert.True(result);
		}

		[Fact]
		public async Task UserLogic_CreateAsync_StorageReturnsNull_ReturnsFalse()
		{
			var (logic, storage, _) = BuildUserLogic();
			var model = new UserBindingModel { Login = "x@test.ru", Fullname = "X" };
			storage.Setup(s => s.GetElementAsync(It.IsAny<UserSearchModel>()))
				   .ReturnsAsync((UserViewModel?)null);
			storage.Setup(s => s.InsertAsync(model))
				   .ReturnsAsync((UserViewModel?)null);

			var result = await logic.CreateAsync(model);

			Assert.False(result);
		}

		[Fact]
		public async Task UserLogic_UpdateAsync_DeactivateWithPendingDocs_ThrowsInvalidOperation()
		{
			var (logic, storage, dusMock) = BuildUserLogic();
			var model = new UserBindingModel
			{
				Id = 5,
				Login = "active@test.ru",
				Fullname = "Активный",
				IsActive = false // деактивируем
			};
			// Пользователь сейчас активен
			storage.Setup(s => s.GetElementAsync(It.Is<UserSearchModel>(m => m.Login == "active@test.ru")))
				   .ReturnsAsync((UserViewModel?)null);
			storage.Setup(s => s.GetElementAsync(It.Is<UserSearchModel>(m => m.Id == 5)))
				   .ReturnsAsync(new UserViewModel { Id = 5, Login = "active@test.ru", IsActive = true });
			// Есть неподписанные документы
			dusMock.Setup(d => d.CountPendingSigningAssignmentsAsync(5)).ReturnsAsync(3);

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				logic.UpdateAsync(model));
		}

		[Fact]
		public async Task UserLogic_UpdateAsync_UserNotFound_ReturnsFalse()
		{
			var (logic, storage, _) = BuildUserLogic();
			var model = new UserBindingModel { Id = 999, Login = "ghost@test.ru", Fullname = "Призрак" };
			storage.Setup(s => s.GetElementAsync(It.Is<UserSearchModel>(m => m.Login == "ghost@test.ru")))
				   .ReturnsAsync((UserViewModel?)null);
			storage.Setup(s => s.GetElementAsync(It.Is<UserSearchModel>(m => m.Id == 999)))
				   .ReturnsAsync((UserViewModel?)null);

			var result = await logic.UpdateAsync(model);

			Assert.False(result);
		}

		[Fact]
		public async Task UserLogic_DeleteAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildUserLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.DeleteAsync(null!));
		}

		[Fact]
		public async Task UserLogic_DeleteAsync_ActiveUserWithPendingDocs_ThrowsInvalidOperation()
		{
			var (logic, storage, dusMock) = BuildUserLogic();
			var model = new UserBindingModel { Id = 7 };
			storage.Setup(s => s.GetElementAsync(It.Is<UserSearchModel>(m => m.Id == 7)))
				   .ReturnsAsync(new UserViewModel { Id = 7, IsActive = true });
			dusMock.Setup(d => d.CountPendingSigningAssignmentsAsync(7)).ReturnsAsync(2);

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				logic.DeleteAsync(model));
		}

		[Fact]
		public async Task UserLogic_DeleteAsync_NotFound_ReturnsFalse()
		{
			var (logic, storage, _) = BuildUserLogic();
			var model = new UserBindingModel { Id = 888 };
			storage.Setup(s => s.GetElementAsync(It.Is<UserSearchModel>(m => m.Id == 888)))
				   .ReturnsAsync((UserViewModel?)null);

			var result = await logic.DeleteAsync(model);

			Assert.False(result);
		}

		[Fact]
		public async Task UserLogic_DeleteAsync_ValidInactiveUser_ReturnsTrue()
		{
			var (logic, storage, _) = BuildUserLogic();
			var model = new UserBindingModel { Id = 10 };
			storage.Setup(s => s.GetElementAsync(It.Is<UserSearchModel>(m => m.Id == 10)))
				   .ReturnsAsync(new UserViewModel { Id = 10, IsActive = false });
			storage.Setup(s => s.DeleteAsync(model))
				   .ReturnsAsync(new UserViewModel { Id = 10, IsActive = false });

			var result = await logic.DeleteAsync(model);

			Assert.True(result);
		}
	}
}
