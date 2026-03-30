using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using Microsoft.EntityFrameworkCore;
using Models;
using Storage.Models;
using System;
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
				if (element.IsDeleted)
				{
					return element.GetViewModel;
				}
				element.IsDeleted = true;
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
			var query = context.Documents.AsQueryable();
			if (!model.IsDeleted.HasValue || model.IsDeleted.Value == false)
			{
				query = query.Where(x => !x.IsDeleted);
			}
			var element = await query.FirstOrDefaultAsync(x =>
				(!string.IsNullOrEmpty(model.Title) && x.Title == model.Title) ||
				(model.Id.HasValue && x.Id == model.Id));
			if (element != null)
			{
				return element.GetViewModel;
			}
			return null;
		}

		private static IQueryable<Document> ApplyDocumentFilters(IQueryable<Document> query, DocumentSearchModel model)
		{
			if (!model.IsDeleted.HasValue || model.IsDeleted.Value == false)
				query = query.Where(x => !x.IsDeleted);
			else if (model.IsDeleted == true)
				query = query.Where(x => x.IsDeleted);

			if (model.CreatedByUserId.HasValue)
				query = query.Where(x => x.CreatedByUserId == model.CreatedByUserId.Value);

			if (model.Statuses != null && model.Statuses.Count > 0)
			{
				var statuses = model.Statuses;
				query = query.Where(x => statuses.Contains(x.Status));
			}
			else if (model.Status.HasValue)
				query = query.Where(x => x.Status == model.Status.Value);

			if (!string.IsNullOrEmpty(model.SearchText))
			{
				var s = model.SearchText;
				query = query.Where(x => x.Title.Contains(s) ||
					(x.Description != null && x.Description.Contains(s)));
			}
			else
			{
				if (!string.IsNullOrEmpty(model.Title))
					query = query.Where(x => x.Title.Contains(model.Title));
				if (!string.IsNullOrEmpty(model.Description))
					query = query.Where(x => x.Description.Contains(model.Description));
			}

			return query;
		}

		public async Task<List<DocumentViewModel>> GetFilteredListAsync(DocumentSearchModel model)
		{
			using var context = new StorageContext();
			var query = ApplyDocumentFilters(context.Documents.AsQueryable(), model);
			query = query.OrderByDescending(x => x.CreatedAt);
			return await query.Select(x => x.GetViewModel).ToListAsync();
		}

		public async Task<(List<DocumentViewModel> Items, int TotalCount)> GetFilteredPagedListAsync(DocumentSearchModel model)
		{
			if (!model.PageNumber.HasValue || !model.PageSize.HasValue || model.PageNumber < 1 || model.PageSize < 1)
				return ([], 0);

			using var context = new StorageContext();
			var query = ApplyDocumentFilters(context.Documents.AsQueryable(), model);
			query = query.OrderByDescending(x => x.CreatedAt);
			var totalCount = await query.CountAsync();
			var skip = (model.PageNumber.Value - 1) * model.PageSize.Value;
			var items = await query
				.Skip(skip)
				.Take(model.PageSize.Value)
				.Select(x => x.GetViewModel)
				.ToListAsync();
			return (items, totalCount);
		}

		public async Task<List<DocumentViewModel>> GetFullListAsync()
		{
			using var context = new StorageContext();
			return await context.Documents
				.Where(x => !x.IsDeleted)
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<DocumentViewModel>> GetPagedListAsync(DocumentSearchModel model)
		{
			var (items, _) = await GetFilteredPagedListAsync(model);
			return items;
		}

		public async Task<DocumentViewModel?> InsertAsync(DocumentBindingModel model)
		{
			var newDocument = Document.Create(model);
			if (newDocument == null)
			{
				return null;
			}
			using var context = new StorageContext();
			newDocument.IsDeleted = false;
			await context.Documents.AddAsync(newDocument);
			await context.SaveChangesAsync();
			if (model.UserIds != null && model.UserIds.Count > 0)
			{
				var userIds = model.IsSequential
					? model.UserIds
					: model.UserIds.Distinct().ToList();

				var activeUserIds = await context.Users
					.Where(x => userIds.Contains(x.Id) && x.IsActive)
					.Select(x => x.Id)
					.ToListAsync();
				if (activeUserIds.Count != userIds.Distinct().Count())
				{
					throw new InvalidOperationException("Нельзя назначить неактивного пользователя на подписание");
				}

				// При последовательном режиме порядок в списке = порядок подписания (1-based).
				// При параллельном режиме Order = 0 (очерёдности нет).
				var documentUsers = userIds
					.Select((userId, index) => new DocumentUser
					{
						UserId = userId,
						DocumentId = newDocument.Id,
						SigningStatus = SigningStatus.NOT_SIGNED,
						AssignedAt = null,
						Order = model.IsSequential ? index + 1 : 0,
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

