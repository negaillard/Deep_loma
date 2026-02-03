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
	public class RoleLogic : IRoleLogic
	{
		private readonly IRoleStorage _RoleStorage;
		public RoleLogic(IRoleStorage RoleStorage)
		{
			_RoleStorage = RoleStorage;
		}
		public async Task<List<RoleViewModel>?> ReadListAsync(RoleSearchModel? model)
		{
			var list = model == null
				? await _RoleStorage.GetFullListAsync()
				: await _RoleStorage.GetFilteredListAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}
		public async Task<List<RoleViewModel>?> ReadPagedListAsync(RoleSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			if (!model.PageNumber.HasValue || !model.PageSize.HasValue)
			{
				throw new ArgumentException("Не указаны параметры пагинации");
			}
			var list = await _RoleStorage.GetPagedListAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}
		public async Task<List<RoleViewModel>?> ReadListByNameContainsAsync(RoleSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			var list = await _RoleStorage.GetFilteredListByNameContainsAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}
		public async Task<RoleViewModel?> ReadElementAsync(RoleSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			var element = await _RoleStorage.GetElementAsync(model);
			if (element == null)
			{
				return null;
			}
			return element;
		}
		public async Task<bool> CreateAsync(RoleBindingModel model)
		{
			await CheckModelAsync(model);
			if (await _RoleStorage.InsertAsync(model) == null)
			{
				return false;
			}
			return true;
		}
		public async Task<bool> UpdateAsync(RoleBindingModel model)
		{
			await CheckModelAsync(model);
			if (await _RoleStorage.UpdateAsync(model) == null)
			{
				return false;
			}
			return true;
		}
		public async Task<bool> DeleteAsync(RoleBindingModel model)
		{
			await CheckModelAsync(model, false);
			if (await _RoleStorage.DeleteAsync(model) == null)
			{
				return false;
			}
			return true;
		}
		private async Task CheckModelAsync(RoleBindingModel model, bool withParams = true)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			if (!withParams)
			{
				return;
			}
			if (string.IsNullOrEmpty(model.Name))
			{
				throw new ArgumentNullException("Нет названия кафедры",
			   nameof(model.Name));
			}
			var element = await _RoleStorage.GetElementAsync(new RoleSearchModel
			{
				Name = model.Name,
			});

			if (element != null && element.Id != model.Id)
			{
				throw new InvalidOperationException("Такая кафедра на факультете уже есть");
			}
		}
	}
}
