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
	public interface IUserLogic
	{
		Task<List<UserViewModel>?> ReadListAsync(UserSearchModel? model);
		Task<List<UserViewModel>?> ReadPagedListAsync(UserSearchModel model);
		Task<List<UserViewModel>?> ReadListByFullnameContainsAsync(UserSearchModel model);
		Task<UserViewModel?> ReadElementAsync(UserSearchModel model);
		Task<bool> CreateAsync(UserBindingModel model);
		Task<bool> UpdateAsync(UserBindingModel model);
		Task<bool> DeleteAsync(UserBindingModel model);
	}
}
