using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Contracts.LogicContracts
{
	public interface ISignatureLogic
	{
		Task<List<SignatureViewModel>?> ReadListAsync(SignatureSearchModel? model);
		Task<List<SignatureViewModel>?> ReadPagedListAsync(SignatureSearchModel model);
		Task<SignatureViewModel?> ReadElementAsync(SignatureSearchModel model);
		Task<bool> CreateAsync(SignatureBindingModel model, Stream file);
		Task<bool> UpdateAsync(SignatureBindingModel model);
		Task<bool> DeleteAsync(SignatureBindingModel model);
	}
}
