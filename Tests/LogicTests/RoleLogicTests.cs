using Contracts.BindingModels;
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
	public class RoleLogicTests
	{
		private static (RoleLogic logic, Mock<IRoleStorage> roleMock, Mock<IUserStorage> userMock)
		BuildRoleLogic()
		{
			var roleMock = new Mock<IRoleStorage>();
			var userMock = new Mock<IUserStorage>();
			var logic = new RoleLogic(roleMock.Object, userMock.Object);
			return (logic, roleMock, userMock);
		}

		[Fact]
		public async Task RoleLogic_ReadListAsync_NullModel_ReturnsFullList()
		{
			var (logic, roleMock, _) = BuildRoleLogic();
			roleMock.Setup(s => s.GetFullListAsync())
					.ReturnsAsync(new List<RoleViewModel> { new() { Id = 1, Name = "Роль" } });

			var result = await logic.ReadListAsync(null);

			roleMock.Verify(s => s.GetFullListAsync(), Times.Once);
			Assert.Single(result!);
		}

		[Fact]
		public async Task RoleLogic_CreateAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildRoleLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.CreateAsync(null!));
		}

		[Fact]
		public async Task RoleLogic_CreateAsync_EmptyName_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildRoleLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.CreateAsync(new RoleBindingModel { Name = "" }));
		}

		[Fact]
		public async Task RoleLogic_CreateAsync_DuplicateName_ThrowsInvalidOperationException()
		{
			var (logic, roleMock, _) = BuildRoleLogic();
			var model = new RoleBindingModel { Id = 0, Name = "Дубликат" };
			roleMock.Setup(s => s.GetElementAsync(It.Is<RoleSearchModel>(m => m.Name == "Дубликат")))
					.ReturnsAsync(new RoleViewModel { Id = 99, Name = "Дубликат" });

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				logic.CreateAsync(model));
		}

		[Fact]
		public async Task RoleLogic_CreateAsync_ValidModel_ReturnsTrue()
		{
			var (logic, roleMock, _) = BuildRoleLogic();
			var model = new RoleBindingModel { Id = 0, Name = "Новая роль", Description = "Описание" };
			roleMock.Setup(s => s.GetElementAsync(It.IsAny<RoleSearchModel>()))
					.ReturnsAsync((RoleViewModel?)null);
			roleMock.Setup(s => s.InsertAsync(model))
					.ReturnsAsync(new RoleViewModel { Id = 1, Name = "Новая роль" });

			var result = await logic.CreateAsync(model);

			Assert.True(result);
		}

		[Fact]
		public async Task RoleLogic_DeleteAsync_NoRoleName_ThrowsInvalidOperation()
		{
			var (logic, roleMock, _) = BuildRoleLogic();
			var model = new RoleBindingModel { Id = 5 };
			// Пытаемся удалить служебную роль «Нет роли»
			roleMock.Setup(s => s.GetElementAsync(It.Is<RoleSearchModel>(m => m.Id == 5)))
					.ReturnsAsync(new RoleViewModel { Id = 5, Name = "Нет роли" });

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				logic.DeleteAsync(model));
		}

		[Fact]
		public async Task RoleLogic_DeleteAsync_NoRoleNotFound_ThrowsInvalidOperation()
		{
			var (logic, roleMock, userMock) = BuildRoleLogic();
			var model = new RoleBindingModel { Id = 10 };
			roleMock.Setup(s => s.GetElementAsync(It.Is<RoleSearchModel>(m => m.Id == 10)))
					.ReturnsAsync(new RoleViewModel { Id = 10, Name = "Обычная роль" });
			// Служебная роль-заглушка «Нет роли» не найдена в БД
			roleMock.Setup(s => s.GetElementAsync(It.Is<RoleSearchModel>(m => m.Name == "Нет роли")))
					.ReturnsAsync((RoleViewModel?)null);

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				logic.DeleteAsync(model));
		}

		[Fact]
		public async Task RoleLogic_DeleteAsync_Valid_ReassignsUsersAndDeletes()
		{
			var (logic, roleMock, userMock) = BuildRoleLogic();
			var model = new RoleBindingModel { Id = 3 };

			roleMock.Setup(s => s.GetElementAsync(It.Is<RoleSearchModel>(m => m.Id == 3)))
					.ReturnsAsync(new RoleViewModel { Id = 3, Name = "Удаляемая" });

			var noRole = new RoleViewModel { Id = 1, Name = "Нет роли" };
			roleMock.Setup(s => s.GetElementAsync(It.Is<RoleSearchModel>(m => m.Name == "Нет роли")))
					.ReturnsAsync(noRole);

			var affectedUser = new UserViewModel
			{
				Id = 42,
				Fullname = "Иван",
				Login = "ivan@test.ru",
				Email = "ivan@test.ru",
				RoleId = 3,
				CertificateId = 1,
				SystemRole = SystemRole.Signer,
				Created = DateTime.UtcNow,
				IsActive = true
			};
			userMock.Setup(s => s.GetFilteredListAsync(It.Is<UserSearchModel>(m => m.RoleId == 3)))
					.ReturnsAsync(new List<UserViewModel> { affectedUser });
			userMock.Setup(s => s.UpdateAsync(It.Is<UserBindingModel>(u => u.Id == 42 && u.RoleId == 1)))
					.ReturnsAsync(new UserViewModel { Id = 42, RoleId = 1 });

			roleMock.Setup(s => s.DeleteAsync(model))
					.ReturnsAsync(new RoleViewModel { Id = 3 });

			var result = await logic.DeleteAsync(model);

			Assert.True(result);
			// Убеждаемся, что пользователю переназначена роль «Нет роли»
			userMock.Verify(
				s => s.UpdateAsync(It.Is<UserBindingModel>(u => u.Id == 42 && u.RoleId == 1)),
				Times.Once);
		}

		[Fact]
		public async Task RoleLogic_ReadPagedListAsync_NullModel_ThrowsArgumentNullException()
		{
			var (logic, _, _) = BuildRoleLogic();

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				logic.ReadPagedListAsync(null!));
		}

		[Fact]
		public async Task RoleLogic_ReadPagedListAsync_NoPaginationParams_ThrowsArgumentException()
		{
			var (logic, _, _) = BuildRoleLogic();

			await Assert.ThrowsAsync<ArgumentException>(() =>
				logic.ReadPagedListAsync(new RoleSearchModel()));
		}
	}
}
