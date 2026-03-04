using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.ViewModels;

namespace Contracts.LogicContracts
{
	public interface IDocumentLogic
	{
		Task<List<DocumentViewModel>?> ReadListAsync(DocumentSearchModel? model);
		Task<List<DocumentViewModel>?> ReadPagedListAsync(DocumentSearchModel model);
		Task<DocumentViewModel?> ReadElementAsync(DocumentSearchModel model);
		Task<bool> CreateAsync(DocumentBindingModel model, Stream file);
		Task<bool> UpdateAsync(DocumentBindingModel model);
		Task<bool> DeleteAsync(DocumentBindingModel model);
	}
}
