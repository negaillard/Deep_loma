using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.ViewModels;
using Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.StorageContracts
{
	public interface IDocumentUserStorage
	{
		Task<List<DocumentUserViewModel>> GetFullListAsync();
		Task<List<DocumentUserViewModel>> GetFilteredListAsync(DocumentUserSearchModel model);
		Task<List<DocumentUserViewModel>> GetPagedListAsync(DocumentUserSearchModel model);
		Task<DocumentUserViewModel?> GetElementAsync(DocumentUserSearchModel model);
		Task<DocumentUserViewModel?> InsertAsync(DocumentUserBindingModel model);
		Task<DocumentUserViewModel?> UpdateAsync(DocumentUserBindingModel model);
		Task<DocumentUserViewModel?> DeleteAsync(DocumentUserBindingModel model);

		/// <summary>
		/// Возвращает постраничный список документов, назначенных пользователю на подпись.
		/// Для статуса NOT_SIGNED/PENDING автоматически применяется фильтр последовательной подписи.
		/// </summary>
		Task<(List<DocumentForSignViewModel> Items, int TotalCount)> GetPagedForSignAsync(
			int userId, SigningStatus? signingStatus, int pageNumber, int pageSize);

		/// <summary>
		/// Количество назначений пользователю, по которым подпись ещё не завершена (ожидает подписи или очередь).
		/// </summary>
		Task<int> CountPendingSigningAssignmentsAsync(int userId);
	}
}
