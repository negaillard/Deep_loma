using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;

namespace Logic
{
	public class CertificateLogic : ICertificateLogic
	{
		private readonly ICertificateStorage _CertificateStorage;
		private readonly ICertificateGeneratorLogic _generator;

		public CertificateLogic(ICertificateStorage certificateStorage, ICertificateGeneratorLogic generator)
		{
			_CertificateStorage = certificateStorage;
			_generator = generator;
		}

		public async Task<List<CertificateViewModel>?> ReadListAsync(CertificateSearchModel? model)
		{
			var list = model == null
				? await _CertificateStorage.GetFullListAsync()
				: await _CertificateStorage.GetFilteredListAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}

		public async Task<List<CertificateViewModel>?> ReadPagedListAsync(CertificateSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			if (!model.PageNumber.HasValue || !model.PageSize.HasValue)
			{
				throw new ArgumentException("Не указаны параметры пагинации");
			}
			var list = await _CertificateStorage.GetPagedListAsync(model);
			if (list == null)
			{
				return null;
			}
			return list;
		}

		public async Task<CertificateViewModel?> ReadElementAsync(CertificateSearchModel model)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			var element = await _CertificateStorage.GetElementAsync(model);
			if (element == null)
			{
				return null;
			}
			return element;
		}

		public async Task<bool> CreateAsync(CertificateBindingModel model)
		{
			await CheckModelAsync(model);
			if (await _CertificateStorage.InsertAsync(model) == null)
			{
				return false;
			}
			return true;
		}

		public async Task<bool> UpdateAsync(CertificateBindingModel model)
		{
			await CheckModelAsync(model);
			if (await _CertificateStorage.UpdateAsync(model) == null)
			{
				return false;
			}
			return true;
		}

		public async Task<bool> DeleteAsync(CertificateBindingModel model)
		{
			await CheckModelAsync(model, false);
			if (await _CertificateStorage.DeleteAsync(model) == null)
			{
				return false;
			}
			return true;
		}

		public async Task<CertificateViewModel?> GenerateSelfSignedAsync(int userId, string owner, string publisher)
		{
			var model = await _generator.GenerateSelfSignedAsync(userId, owner, publisher);
			await CheckModelAsync(model);
			await DeactivateUserCertificatesAsync(userId);
			return await _CertificateStorage.InsertAsync(model);
		}

		private async Task DeactivateUserCertificatesAsync(int userId)
		{
			var active = await _CertificateStorage.GetFilteredListAsync(new CertificateSearchModel
			{
				UserId = userId,
				IsActual = true,
			});

			foreach (var cert in active)
			{
				await _CertificateStorage.UpdateAsync(new CertificateBindingModel
				{
					Id = cert.Id,
					StartDate = cert.StartDate,
					FinishDate = cert.FinishDate,
					PublicKey = cert.PublicKey,
					Publisher = cert.Publisher,
					Owner = cert.Owner,
					Number = cert.Number,
					UserId = cert.UserId,
					IsActual = false,
					Mode = cert.Mode,
					FilePath = cert.FilePath,
				});
			}
		}

		private async Task CheckModelAsync(CertificateBindingModel model, bool withParams = true)
		{
			if (model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}
			if (!withParams)
			{
				return;
			}
			if (string.IsNullOrEmpty(model.Number))
			{
				throw new ArgumentNullException("Нет номера сертификата",
					nameof(model.Number));
			}
			if (model.UserId <= 0)
			{
				throw new ArgumentException("Не указан пользователь",
					nameof(model.UserId));
			}
			var element = await _CertificateStorage.GetElementAsync(new CertificateSearchModel
			{
				Number = model.Number,
			});
			if (element != null && element.Id != model.Id)
			{
				throw new InvalidOperationException("Такой сертификат уже есть");
			}
		}
	}
}





