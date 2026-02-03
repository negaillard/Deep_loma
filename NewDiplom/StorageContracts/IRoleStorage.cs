using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.StorageContracts
{
	public interface IRoleStorage
	{
		Task<List<RoleViewModel>> GetFullListAsync();
		Task<List<RoleViewModel>> GetFilteredListAsync(RoleSearchModel model);
		Task<List<RoleViewModel>> GetFilteredListByNameContainsAsync(RoleSearchModel model);
		Task<List<RoleViewModel>> GetPagedListAsync(RoleSearchModel model);
		Task<RoleViewModel?> GetElementAsync(RoleSearchModel model);
		Task<RoleViewModel?> InsertAsync(RoleBindingModel model);
		Task<RoleViewModel?> UpdateAsync(RoleBindingModel model);
		Task<RoleViewModel?> DeleteAsync(RoleBindingModel model);
	}
}
