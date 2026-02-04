using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Logic
{
	public class DocumentUserLogic : IDocumentUserLogic
	{
		private readonly IDocumentUserStorage _DocumentUserStorage;
		public DocumentUserLogic(IDocumentUserStorage DocumentUserStorage)
		{
			_DocumentUserStorage = DocumentUserStorage;
		}

		public async Task<List<DocumentUserViewModel>?> ReadListAsync(DocumentUserSearchModel? model)
		{
			var list = model == null
				? await _DocumentUserStorage.GetFullListAsync()
				: await _DocumentUserStorage.GetFilteredListAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}

		public async Task<List<DocumentUserViewModel>?> ReadPagedListAsync(DocumentUserSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			if (!model.PageNumber.HasValue || !model.PageSize.HasValue)
			{
				throw new ArgumentException("Не указаны параметры пагинации");
			}
			var list = await _DocumentUserStorage.GetPagedListAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}

		public async Task<DocumentUserViewModel?> ReadElementAsync(DocumentUserSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			var element = await _DocumentUserStorage.GetElementAsync(model);
			if (element == null)
			{
				return null;
			}
			return element;
		}

		public async Task<bool> CreateAsync(DocumentUserBindingModel model)
		{
			await CheckModelAsync(model);
			if (await _DocumentUserStorage.InsertAsync(model) == null)
			{
				return false;
			}
			return true;
		}

		public async Task<bool> UpdateAsync(DocumentUserBindingModel model)
		{
			await CheckModelAsync(model);
			if (await _DocumentUserStorage.UpdateAsync(model) == null)
			{
				return false;
			}
			return true;
		}

		public async Task<bool> DeleteAsync(DocumentUserBindingModel model)
		{
			await CheckModelAsync(model, false);
			if (await _DocumentUserStorage.DeleteAsync(model) == null)
			{
				return false;
			}
			return true;
		}

		private async Task CheckModelAsync(DocumentUserBindingModel model, bool withParams = true)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			if (!withParams)
			{
				return;
			}
			if (model.UserId <= 0)
			{
				throw new ArgumentException("Не указан пользователь",
					nameof(model.UserId));
			}
			if (model.DocumentId <= 0)
			{
				throw new ArgumentException("Не указан документ",
					nameof(model.DocumentId));
			}
			var element = await _DocumentUserStorage.GetElementAsync(new DocumentUserSearchModel
			{
				UserId = model.UserId,
				DocumentId = model.DocumentId,
			});
			if (element != null && element.Id != model.Id)
			{
				throw new InvalidOperationException("Пользователь уже назначен на этот документ");
			}
		}
	}
}

