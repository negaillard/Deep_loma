using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;

namespace Logic
{
	public class DocumentLogic : IDocumentLogic
	{
		private readonly IDocumentStorage _documentStorage;
		private readonly IFileStorage _fileStorage;
		public DocumentLogic(IDocumentStorage DocumentStorage, IFileStorage FileStorage)
		{
			_documentStorage = DocumentStorage;
			_fileStorage = FileStorage;
		}
		public async Task<List<DocumentViewModel>?> ReadListAsync(DocumentSearchModel? model)
		{
			var list = model == null
				? await _documentStorage.GetFullListAsync()
				: await _documentStorage.GetFilteredListAsync(model);
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
			var list = await _documentStorage.GetPagedListAsync(model);
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
			var element = await _documentStorage.GetElementAsync(model);
			if (element == null)
			{
				return null;
			}
			return element;
		}
		/// <summary>
		/// работа с файловым хранилищем
		/// </summary>
		public async Task<bool> CreateAsync(DocumentBindingModel model, Stream file)
		{
			await CheckModelAsync(model);
			var created = await _documentStorage.InsertAsync(model);
			if (created == null)
			{
				return false;
			}
			model.Id = created.Id;
			model.Path = await _fileStorage.SaveOriginalAsync(created.Id, file);
			if (await _documentStorage.UpdateAsync(model) == null)
			{
				return false;
			}
			return true;
		}
		public async Task<bool> UpdateAsync(DocumentBindingModel model)
		{
			await CheckModelAsync(model);
			if (await _documentStorage.UpdateAsync(model) == null)
			{
				return false;
			}
			return true;
		}
		public async Task<bool> DeleteAsync(DocumentBindingModel model)
		{
			await CheckModelAsync(model, false);
			if (await _documentStorage.DeleteAsync(model) == null)
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
			var element = await _documentStorage.GetElementAsync(new DocumentSearchModel
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

