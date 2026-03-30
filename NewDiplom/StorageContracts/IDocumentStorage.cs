using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.ViewModels;

namespace Contracts.StorageContracts
{
	public interface IDocumentStorage
	{
		Task<List<DocumentViewModel>> GetFullListAsync();
		Task<List<DocumentViewModel>> GetFilteredListAsync(DocumentSearchModel model);
		Task<(List<DocumentViewModel> Items, int TotalCount)> GetFilteredPagedListAsync(DocumentSearchModel model);
		Task<List<DocumentViewModel>> GetPagedListAsync(DocumentSearchModel model);
		Task<DocumentViewModel?> GetElementAsync(DocumentSearchModel model);
		Task<DocumentViewModel?> InsertAsync(DocumentBindingModel model);
		Task<DocumentViewModel?> UpdateAsync(DocumentBindingModel model);
		Task<DocumentViewModel?> DeleteAsync(DocumentBindingModel model);
	}
}
