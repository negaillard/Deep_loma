using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic
{
	public class UserLogic : IUserLogic
	{
		private readonly IUserStorage _UserStorage;
		public UserLogic(IUserStorage UserStorage)
		{
			_UserStorage = UserStorage;
		}
		public async Task<List<UserViewModel>?> ReadListAsync(UserSearchModel? model)
		{
			var list = model == null
				? await _UserStorage.GetFullListAsync()
				: await _UserStorage.GetFilteredListAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}
		public async Task<List<UserViewModel>?> ReadPagedListAsync(UserSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			if (!model.PageNumber.HasValue || !model.PageSize.HasValue)
			{
				throw new ArgumentException("Не указаны параметры пагинации");
			}
			var list = await _UserStorage.GetPagedListAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}
		public async Task<List<UserViewModel>?> ReadListByFullnameContainsAsync(UserSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			var list = await _UserStorage.GetFilteredListByFullnameContainsAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}
		public async Task<UserViewModel?> ReadElementAsync(UserSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			var element = await _UserStorage.GetElementAsync(model);
			if (element == null)
			{
				return null;
			}
			return element;
		}
		public async Task<bool> CreateAsync(UserBindingModel model)
		{
			await CheckModelAsync(model);
			if (await _UserStorage.InsertAsync(model) == null)
			{
				return false;
			}
			return true;
		}
		public async Task<bool> UpdateAsync(UserBindingModel model)
		{
			await CheckModelAsync(model);
			if (await _UserStorage.UpdateAsync(model) == null)
			{
				return false;
			}
			return true;
		}
		public async Task<bool> DeleteAsync(UserBindingModel model)
		{
			await CheckModelAsync(model, false);
			if (await _UserStorage.DeleteAsync(model) == null)
			{
				return false;
			}
			return true;
		}
		private async Task CheckModelAsync(UserBindingModel model, bool withParams = true)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			if (!withParams)
			{
				return;
			}
			if (string.IsNullOrEmpty(model.Login))
			{
				throw new ArgumentNullException("Нет названия кафедры",
			   nameof(model.Login));
			}
			var element = await _UserStorage.GetElementAsync(new UserSearchModel
			{
				Login = model.Login,
			});

			if (element != null && element.Id != model.Id)
			{
				throw new InvalidOperationException("Такая кафедра на факультете уже есть");
			}
		}
	}
}
