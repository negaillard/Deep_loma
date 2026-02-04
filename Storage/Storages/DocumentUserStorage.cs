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
	public class DocumentUserStorage : IDocumentUserStorage
	{
		public async Task<DocumentUserViewModel?> DeleteAsync(DocumentUserBindingModel model)
		{
			using var context = new StorageContext();
			var element = await context.DocumentUsers.FirstOrDefaultAsync(rec => rec.Id == model.Id);
			if (element != null)
			{
				context.DocumentUsers.Remove(element);
				await context.SaveChangesAsync();
				return element.GetViewModel;
			}
			return null;
		}

		public async Task<DocumentUserViewModel?> GetElementAsync(DocumentUserSearchModel model)
		{
			if (!model.Id.HasValue && !model.UserId.HasValue && !model.DocumentId.HasValue)
			{
				return null;
			}
			using var context = new StorageContext();
			var element = await context.DocumentUsers
				.FirstOrDefaultAsync(x =>
					(model.Id.HasValue && x.Id == model.Id) ||
					(model.UserId.HasValue && model.DocumentId.HasValue &&
						x.UserId == model.UserId && x.DocumentId == model.DocumentId));
			if (element != null)
			{
				return element.GetViewModel;
			}
			return null;
		}

		public async Task<List<DocumentUserViewModel>> GetFilteredListAsync(DocumentUserSearchModel model)
		{
			if (!model.UserId.HasValue &&
				!model.DocumentId.HasValue &&
				!model.SigningStatus.HasValue &&
				!model.AssignedAt.HasValue)
			{
				return new();
			}
			using var context = new StorageContext();
			var query = context.DocumentUsers.AsQueryable();
			if (model.UserId.HasValue)
			{
				query = query.Where(x => x.UserId == model.UserId);
			}
			if (model.DocumentId.HasValue)
			{
				query = query.Where(x => x.DocumentId == model.DocumentId);
			}
			if (model.SigningStatus.HasValue)
			{
				query = query.Where(x => x.SigningStatus == model.SigningStatus.Value);
			}
			if (model.AssignedAt.HasValue)
			{
				query = query.Where(x => x.AssignedAt == model.AssignedAt.Value);
			}
			return await query
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<DocumentUserViewModel>> GetFullListAsync()
		{
			using var context = new StorageContext();
			return await context.DocumentUsers
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<DocumentUserViewModel>> GetPagedListAsync(DocumentUserSearchModel model)
		{
			if (!model.PageNumber.HasValue || !model.PageSize.HasValue || model.PageNumber < 1 || model.PageSize < 1)
			{
				return new();
			}
			var skip = (model.PageNumber.Value - 1) * model.PageSize.Value;
			using var context = new StorageContext();
			return await context.DocumentUsers
				.OrderBy(x => x.Id)
				.Skip(skip)
				.Take(model.PageSize.Value)
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<DocumentUserViewModel?> InsertAsync(DocumentUserBindingModel model)
		{
			var newDocumentUser = DocumentUser.Create(model);
			if (newDocumentUser == null)
			{
				return null;
			}
			using var context = new StorageContext();
			await context.DocumentUsers.AddAsync(newDocumentUser);
			await context.SaveChangesAsync();
			return newDocumentUser.GetViewModel;
		}

		public async Task<DocumentUserViewModel?> UpdateAsync(DocumentUserBindingModel model)
		{
			using var context = new StorageContext();
			var documentUser = await context.DocumentUsers.FirstOrDefaultAsync(x => x.Id == model.Id);
			if (documentUser == null)
			{
				return null;
			}
			documentUser.Update(model);
			await context.SaveChangesAsync();
			return documentUser.GetViewModel;
		}
	}
}
