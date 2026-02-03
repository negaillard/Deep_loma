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
	public interface IUserStorage
	{
		Task<List<UserViewModel>> GetFullListAsync();
		Task<List<UserViewModel>> GetFilteredListAsync(UserSearchModel model);
		Task<List<UserViewModel>> GetFilteredListByFullnameContainsAsync(UserSearchModel model);
		Task<List<UserViewModel>> GetPagedListAsync(UserSearchModel model);
		Task<UserViewModel?> GetElementAsync(UserSearchModel model);
		Task<UserViewModel?> InsertAsync(UserBindingModel model);
		Task<UserViewModel?> UpdateAsync(UserBindingModel model);
		Task<UserViewModel?> DeleteAsync(UserBindingModel model);
	}
}
