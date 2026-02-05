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
	public interface ISignatureStorage
	{
		Task<List<SignatureViewModel>> GetFullListAsync();
		Task<List<SignatureViewModel>> GetFilteredListAsync(SignatureSearchModel model);
		Task<List<SignatureViewModel>> GetPagedListAsync(SignatureSearchModel model);
		Task<SignatureViewModel?> GetElementAsync(SignatureSearchModel model);
		Task<SignatureViewModel?> InsertAsync(SignatureBindingModel model);
		Task<SignatureViewModel?> UpdateAsync(SignatureBindingModel model);
		Task<SignatureViewModel?> DeleteAsync(SignatureBindingModel model);
	}
}
