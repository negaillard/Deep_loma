using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.ViewModels;

namespace Contracts.StorageContracts
{
	public interface ICertificateStorage
	{
		Task<List<CertificateViewModel>> GetFullListAsync();
		Task<List<CertificateViewModel>> GetFilteredListAsync(CertificateSearchModel model);
		Task<List<CertificateViewModel>> GetPagedListAsync(CertificateSearchModel model);
		Task<CertificateViewModel?> GetElementAsync(CertificateSearchModel model);
		Task<CertificateViewModel?> InsertAsync(CertificateBindingModel model);
		Task<CertificateViewModel?> UpdateAsync(CertificateBindingModel model);
		Task<CertificateViewModel?> DeleteAsync(CertificateBindingModel model);
	}
}



