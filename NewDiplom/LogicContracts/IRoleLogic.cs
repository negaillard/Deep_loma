using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.LogicContracts
{
	public interface IRoleLogic
	{
		Task<List<RoleViewModel>?> ReadListAsync(RoleSearchModel? model);
		Task<List<RoleViewModel>?> ReadPagedListAsync(RoleSearchModel model);
		Task<List<RoleViewModel>?> ReadListByNameContainsAsync(RoleSearchModel model);
		Task<RoleViewModel?> ReadElementAsync(RoleSearchModel model);
		Task<bool> CreateAsync(RoleBindingModel model);
		Task<bool> UpdateAsync(RoleBindingModel model);
		Task<bool> DeleteAsync(RoleBindingModel model);
	}
}
