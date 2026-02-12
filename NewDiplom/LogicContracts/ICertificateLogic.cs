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
	public interface ICertificateLogic
	{
		Task<List<CertificateViewModel>?> ReadListAsync(CertificateSearchModel? model);
		Task<List<CertificateViewModel>?> ReadPagedListAsync(CertificateSearchModel model);
		Task<CertificateViewModel?> ReadElementAsync(CertificateSearchModel model);
		Task<bool> CreateAsync(CertificateBindingModel model);
		Task<bool> UpdateAsync(CertificateBindingModel model);
		Task<bool> DeleteAsync(CertificateBindingModel model);
	}
}
