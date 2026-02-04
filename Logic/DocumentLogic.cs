using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;

namespace Logic
{
	public class DocumentLogic : IDocumentLogic
	{
		private readonly IDocumentStorage _DocumentStorage;
		public DocumentLogic(IDocumentStorage DocumentStorage)
		{
			_DocumentStorage = DocumentStorage;
		}
		public async Task<List<DocumentViewModel>?> ReadListAsync(DocumentSearchModel? model)
		{
			var list = model == null
				? await _DocumentStorage.GetFullListAsync()
				: await _DocumentStorage.GetFilteredListAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}

		public async Task<List<DocumentViewModel>?> ReadPagedListAsync(DocumentSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			if (!model.PageNumber.HasValue || !model.PageSize.HasValue)
			{
				throw new ArgumentException("Не указаны параметры пагинации");
			}
			var list = await _DocumentStorage.GetPagedListAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}
		public async Task<DocumentViewModel?> ReadElementAsync(DocumentSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			var element = await _DocumentStorage.GetElementAsync(model);
			if (element == null)
			{
				return null;
			}
			return element;
		}
		public async Task<bool> CreateAsync(DocumentBindingModel model)
		{
			await CheckModelAsync(model);
			if (await _DocumentStorage.InsertAsync(model) == null)
			{
				return false;
			}
			return true;
		}
		public async Task<bool> UpdateAsync(DocumentBindingModel model)
		{
			await CheckModelAsync(model);
			if (await _DocumentStorage.UpdateAsync(model) == null)
			{
				return false;
			}
			return true;
		}
		public async Task<bool> DeleteAsync(DocumentBindingModel model)
		{
			await CheckModelAsync(model, false);
			if (await _DocumentStorage.DeleteAsync(model) == null)
			{
				return false;
			}
			return true;
		}
		private async Task CheckModelAsync(DocumentBindingModel model, bool withParams = true)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			if (!withParams)
			{
				return;
			}
			if (string.IsNullOrEmpty(model.Title))
			{
				throw new ArgumentNullException("Нет названия документа",
			   nameof(model.Title));
			}
			var element = await _DocumentStorage.GetElementAsync(new DocumentSearchModel
			{
				Title = model.Title,
			});

			if (element != null && element.Id != model.Id)
			{
				throw new InvalidOperationException("Такой документ уже есть");
			}
		}
	}
}

