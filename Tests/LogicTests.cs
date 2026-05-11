using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using Logic;
using Models;
using Moq;
using Xunit;

namespace Tests;

/// <summary>
/// Юнит-тесты для слоя Logic (UserLogic, RoleLogic, DocumentLogic).
/// Все зависимости (IUserStorage, IRoleStorage, ...) заменены моками через Moq.
/// </summary>
public class LogicTests
{
    // ─────────────────────────── UserLogic ──────────────────────────────

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

    // ─────────────────────────── RoleLogic ──────────────────────────────

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
            Id = 42, Fullname = "Иван", Login = "ivan@test.ru", Email = "ivan@test.ru",
            RoleId = 3, CertificateId = 1, SystemRole = SystemRole.Signer,
            Created = DateTime.UtcNow, IsActive = true
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

    // ─────────────────────────── DocumentLogic ──────────────────────────

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
