using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using Microsoft.EntityFrameworkCore;
using Storage.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Storage.Storages
{
	public class SignatureStorage : ISignatureStorage
	{
		private readonly StorageContext _context;

		public SignatureStorage(StorageContext context)
		{
			_context = context;
		}

		public async Task<SignatureViewModel?> DeleteAsync(SignatureBindingModel model)
		{
			var element = await _context.Signatures.FirstOrDefaultAsync(rec => rec.Id == model.Id);
			if (element != null)
			{
				if (element.IsDeleted)
				{
					return element.GetViewModel;
				}
				element.IsDeleted = true;
				await _context.SaveChangesAsync();
				return element.GetViewModel;
			}
			return null;
		}

		public async Task<SignatureViewModel?> GetElementAsync(SignatureSearchModel model)
		{
			if (!model.Id.HasValue &&
				string.IsNullOrEmpty(model.SignatureValue) &&
				!model.UserId.HasValue &&
				!model.DocumentId.HasValue)
			{
				return null;
			}
			var query = _context.Signatures.AsQueryable();
			if (!model.IsDeleted.HasValue || model.IsDeleted.Value == false)
			{
				query = query.Where(x => !x.IsDeleted);
			}
			var element = await query.FirstOrDefaultAsync(x =>
				(model.Id.HasValue && x.Id == model.Id) ||
				(!string.IsNullOrEmpty(model.SignatureValue) && x.SignatureValue == model.SignatureValue) ||
				(model.UserId.HasValue && model.DocumentId.HasValue &&
					x.UserId == model.UserId && x.DocumentId == model.DocumentId));
			if (element != null)
			{
				return element.GetViewModel;
			}
			return null;
		}

		public async Task<List<SignatureViewModel>> GetFilteredListAsync(SignatureSearchModel model)
		{
			if (string.IsNullOrEmpty(model.SignatureValue) &&
				!model.CerificateId.HasValue &&
				!model.SignedAt.HasValue &&
				!model.UserId.HasValue &&
				!model.DocumentId.HasValue &&
				!model.IsDeleted.HasValue)
			{
				return new();
			}
			var query = _context.Signatures.AsQueryable();
			if (!model.IsDeleted.HasValue || model.IsDeleted.Value == false)
			{
				query = query.Where(x => !x.IsDeleted);
			}
			if (!string.IsNullOrEmpty(model.SignatureValue))
			{
				query = query.Where(x => x.SignatureValue.Contains(model.SignatureValue));
			}
			if (model.CerificateId.HasValue)
			{
				query = query.Where(x => x.CerificateId == model.CerificateId);
			}
			if (model.SignedAt.HasValue)
			{
				query = query.Where(x => x.SignedAt == model.SignedAt.Value);
			}
			if (model.UserId.HasValue)
			{
				query = query.Where(x => x.UserId == model.UserId);
			}
			if (model.DocumentId.HasValue)
			{
				query = query.Where(x => x.DocumentId == model.DocumentId);
			}
			return await query
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<SignatureViewModel>> GetFullListAsync()
		{
			return await _context.Signatures
				.Where(x => !x.IsDeleted)
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<SignatureViewModel>> GetPagedListAsync(SignatureSearchModel model)
		{
			if (!model.PageNumber.HasValue || !model.PageSize.HasValue || model.PageNumber < 1 || model.PageSize < 1)
			{
				return new();
			}
			var skip = (model.PageNumber.Value - 1) * model.PageSize.Value;
			return await _context.Signatures
				.Where(x => !x.IsDeleted)
				.OrderBy(x => x.Id)
				.Skip(skip)
				.Take(model.PageSize.Value)
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<SignatureViewModel?> InsertAsync(SignatureBindingModel model)
		{
			var newSignature = Signature.Create(model);
			if (newSignature == null)
			{
				return null;
			}
			newSignature.IsDeleted = false;
			await _context.Signatures.AddAsync(newSignature);
			await _context.SaveChangesAsync();
			return newSignature.GetViewModel;
		}

		public async Task<SignatureViewModel?> UpdateAsync(SignatureBindingModel model)
		{
			var signature = await _context.Signatures.FirstOrDefaultAsync(x => x.Id == model.Id);
			if (signature == null)
			{
				return null;
			}
			signature.Update(model);
			await _context.SaveChangesAsync();
			return signature.GetViewModel;
		}
	}
}


