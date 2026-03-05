using Contracts;
using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;

namespace Logic
{
	public class RoleLogic : IRoleLogic
	{
		private readonly IRoleStorage _RoleStorage;
		private readonly IUserStorage _UserStorage;

		public RoleLogic(IRoleStorage roleStorage, IUserStorage userStorage)
		{
			_RoleStorage = roleStorage;
			_UserStorage = userStorage;
		}

		public async Task<List<RoleViewModel>?> ReadListAsync(RoleSearchModel? model)
		{
			var list = model == null
				? await _RoleStorage.GetFullListAsync()
				: await _RoleStorage.GetFilteredListAsync(model);
			return list;
		}

		public async Task<List<RoleViewModel>?> ReadPagedListAsync(RoleSearchModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (!model.PageNumber.HasValue || !model.PageSize.HasValue)
				throw new ArgumentException("Не указаны параметры пагинации");

			return await _RoleStorage.GetPagedListAsync(model);
		}

		public async Task<List<RoleViewModel>?> ReadListByNameContainsAsync(RoleSearchModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			return await _RoleStorage.GetFilteredListByNameContainsAsync(model);
		}

		public async Task<RoleViewModel?> ReadElementAsync(RoleSearchModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			return await _RoleStorage.GetElementAsync(model);
		}

		public async Task<bool> CreateAsync(RoleBindingModel model)
		{
			await CheckModelAsync(model);
			return await _RoleStorage.InsertAsync(model) != null;
		}

		public async Task<bool> UpdateAsync(RoleBindingModel model)
		{
			await CheckModelAsync(model);
			return await _RoleStorage.UpdateAsync(model) != null;
		}

		public async Task<bool> DeleteAsync(RoleBindingModel model)
		{
			await CheckModelAsync(model, withParams: false);

			// Запрещаем удаление служебной роли «Нет роли»
			var target = await _RoleStorage.GetElementAsync(new RoleSearchModel { Id = model.Id });
			if (target != null && target.Name == SystemConstants.NoRoleName)
				throw new InvalidOperationException("Нельзя удалить служебную роль «Нет роли».");

			// Находим служебную роль-замену
			var noRole = await _RoleStorage.GetElementAsync(
				new RoleSearchModel { Name = SystemConstants.NoRoleName });

			if (noRole == null)
				throw new InvalidOperationException(
					"Служебная роль «Нет роли» не найдена. Перезапустите приложение.");

			// Переназначаем всех пользователей с удаляемой ролью
			var affectedUsers = await _UserStorage.GetFilteredListAsync(
				new UserSearchModel { RoleId = model.Id });

			foreach (var user in affectedUsers)
			{
				await _UserStorage.UpdateAsync(new UserBindingModel
				{
					Id            = user.Id,
					Fullname      = user.Fullname,
					Login         = user.Login,
					Email         = user.Email,
					SystemRole    = user.SystemRole,
					CertificateId = user.CertificateId,
					Created       = user.Created,
					IsActive      = user.IsActive,
					RoleId        = noRole.Id,
				});
			}

			return await _RoleStorage.DeleteAsync(model) != null;
		}

		private async Task CheckModelAsync(RoleBindingModel model, bool withParams = true)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (!withParams)
				return;

			if (string.IsNullOrEmpty(model.Name))
				throw new ArgumentNullException("Нет названия роли", nameof(model.Name));

			var element = await _RoleStorage.GetElementAsync(new RoleSearchModel { Name = model.Name });

			if (element != null && element.Id != model.Id)
				throw new InvalidOperationException("Роль с таким названием уже существует.");
		}
	}
}
