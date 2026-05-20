using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.ViewModels;
using Models.Enums;

namespace Contracts.LogicContracts
{
	public interface IDocumentUserLogic
	{
		Task<List<DocumentUserViewModel>?> ReadListAsync(DocumentUserSearchModel? model);
		Task<List<DocumentUserViewModel>?> ReadPagedListAsync(DocumentUserSearchModel model);
		Task<DocumentUserViewModel?> ReadElementAsync(DocumentUserSearchModel model);
		Task<bool> CreateAsync(DocumentUserBindingModel model);
		Task<bool> UpdateAsync(DocumentUserBindingModel model);
		Task<bool> DeleteAsync(DocumentUserBindingModel model);

		Task<PagedResult<DocumentForSignViewModel>> GetPagedForSignAsync(
			int userId, SigningStatus? signingStatus, int pageNumber, int pageSize);
	}
}

