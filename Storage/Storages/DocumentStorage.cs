using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using Microsoft.EntityFrameworkCore;
using Models;
using Storage.Models;
using System.Linq;

namespace Storage.Storages
{
	public class DocumentStorage : IDocumentStorage
	{
		public async Task<DocumentViewModel?> DeleteAsync(DocumentBindingModel model)
		{
			using var context = new StorageContext();
			var element = await context.Documents.FirstOrDefaultAsync(rec => rec.Id == model.Id);
			if (element != null)
			{
				context.Documents.Remove(element);
				await context.SaveChangesAsync();
				return element.GetViewModel;
			}
			return null;
		}

		public async Task<DocumentViewModel?> GetElementAsync(DocumentSearchModel model)
		{
			if (string.IsNullOrEmpty(model.Title) && !model.Id.HasValue)
			{
				return null;
			}
			using var context = new StorageContext();
			var element = await context.Documents
				.FirstOrDefaultAsync(x =>
					(!string.IsNullOrEmpty(model.Title) && x.Title == model.Title) ||
					(model.Id.HasValue && x.Id == model.Id));
			if (element != null)
			{
				return element.GetViewModel;
			}
			return null;
		}

		public async Task<List<DocumentViewModel>> GetFilteredListAsync(DocumentSearchModel model)
		{
			if (string.IsNullOrEmpty(model.Title))
			{
				return new();
			}
			using var context = new StorageContext();
			return await context.Documents
				.Where(x => x.Title.Contains(model.Title))
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<DocumentViewModel>> GetFullListAsync()
		{
			using var context = new StorageContext();
			return await context.Documents
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<DocumentViewModel>> GetPagedListAsync(DocumentSearchModel model)
		{
			if (!model.PageNumber.HasValue || !model.PageSize.HasValue || model.PageNumber < 1 || model.PageSize < 1)
			{
				return new();
			}
			var skip = (model.PageNumber.Value - 1) * model.PageSize.Value;
			using var context = new StorageContext();
			return await context.Documents
				.OrderBy(x => x.Id)
				.Skip(skip)
				.Take(model.PageSize.Value)
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<DocumentViewModel?> InsertAsync(DocumentBindingModel model)
		{
			var newDocument = Document.Create(model);
			if (newDocument == null)
			{
				return null;
			}
			using var context = new StorageContext();
			await context.Documents.AddAsync(newDocument);
			await context.SaveChangesAsync();
			if (model.UserIds != null && model.UserIds.Count > 0)
			{
				var documentUsers = model.UserIds
					.Distinct()
					.Select(userId => new DocumentUser
					{
						UserId = userId,
						DocumentId = newDocument.Id,
						SigningStatus = SigningStatus.NOT_SIGNED,
						AssignedAt = null,
					})
					.ToList();
				await context.DocumentUsers.AddRangeAsync(documentUsers);
				await context.SaveChangesAsync();
			}
			return newDocument.GetViewModel;
		}

		public async Task<DocumentViewModel?> UpdateAsync(DocumentBindingModel model)
		{
			using var context = new StorageContext();
			var document = await context.Documents.FirstOrDefaultAsync(x => x.Id == model.Id);
			if (document == null)
			{
				return null;
			}
			document.Update(model);
			await context.SaveChangesAsync();
			return document.GetViewModel;
		}
	}
}

