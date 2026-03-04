using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Logic
{
	public class SignatureLogic : ISignatureLogic
	{
		private readonly ISignatureStorage _SignatureStorage;
		private readonly IFileStorage _fileStorage;
		public SignatureLogic(ISignatureStorage SignatureStorage, IFileStorage fileStorage)
		{
			_SignatureStorage = SignatureStorage;
			_fileStorage = fileStorage;
		}

		public async Task<List<SignatureViewModel>?> ReadListAsync(SignatureSearchModel? model)
		{
			var list = model == null
				? await _SignatureStorage.GetFullListAsync()
				: await _SignatureStorage.GetFilteredListAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}

		public async Task<List<SignatureViewModel>?> ReadPagedListAsync(SignatureSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			if (!model.PageNumber.HasValue || !model.PageSize.HasValue)
			{
				throw new ArgumentException("Не указаны параметры пагинации");
			}
			var list = await _SignatureStorage.GetPagedListAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}

		public async Task<SignatureViewModel?> ReadElementAsync(SignatureSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			var element = await _SignatureStorage.GetElementAsync(model);
			if (element == null)
			{
				return null;
			}
			return element;
		}

		public async Task<bool> CreateAsync(SignatureBindingModel model, Stream file)
		{
			await CheckModelAsync(model);
			var created = await _SignatureStorage.InsertAsync(model);
			if (created == null)
			{
				return false;
			}
			model.Id = created.Id;
			model.Path = await _fileStorage.SaveSignatureAsync(model.DocumentId, created.Id, file);
			if (await _SignatureStorage.UpdateAsync(model) == null)
			{
				return false;
			}
			return true;
		}

		public async Task<bool> UpdateAsync(SignatureBindingModel model)
		{
			await CheckModelAsync(model);
			if (await _SignatureStorage.UpdateAsync(model) == null)
			{
				return false;
			}
			return true;
		}

		public async Task<bool> DeleteAsync(SignatureBindingModel model)
		{
			await CheckModelAsync(model, false);
			if (await _SignatureStorage.DeleteAsync(model) == null)
			{
				return false;
			}
			return true;
		}

		private async Task CheckModelAsync(SignatureBindingModel model, bool withParams = true)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			if (!withParams)
			{
				return;
			}
			if (string.IsNullOrEmpty(model.SignatureValue))
			{
				throw new ArgumentNullException("Нет значения подписи",
					nameof(model.SignatureValue));
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
			var element = await _SignatureStorage.GetElementAsync(new SignatureSearchModel
			{
				UserId = model.UserId,
				DocumentId = model.DocumentId,
			});
			if (element != null && element.Id != model.Id)
			{
				throw new InvalidOperationException("Подпись для пользователя и документа уже есть");
			}
		}
	}
}






