using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using Microsoft.EntityFrameworkCore;
using Models;
using Storage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

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
			await UpdateDocumentStatusAsync(context, documentUser.DocumentId);
			return documentUser.GetViewModel;
		}
		public async Task<(List<DocumentForSignViewModel> Items, int TotalCount)> GetPagedForSignAsync(
			int userId, SigningStatus? signingStatus, int pageNumber, int pageSize)
		{
			using var context = new StorageContext();

			var query = context.DocumentUsers
				.Include(du => du.Document)
				.Where(du => du.UserId == userId && !du.Document.IsDeleted);

			if (signingStatus.HasValue)
				query = query.Where(du => du.SigningStatus == signingStatus.Value);

			// для NOT_SIGNED и PENDING применяем фильтр последовательной подписи на стороне БД
			bool applySeqFilter = !signingStatus.HasValue
				|| signingStatus == SigningStatus.NOT_SIGNED
				|| signingStatus == SigningStatus.PENDING;

			if (applySeqFilter)
			{
				query = query.Where(du =>
					!du.Document.IsSequential ||
					du.Order <= 1 ||
					!context.DocumentUsers.Any(prev =>
						prev.DocumentId == du.DocumentId &&
						prev.Order < du.Order &&
						prev.SigningStatus != SigningStatus.SIGNED));
			}

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(du => du.AssignedAt)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.Select(du => new DocumentForSignViewModel
				{
					Id = du.DocumentId,
					Title = du.Document.Title,
					Description = du.Document.Description,
					CreatedBy = du.Document.CreatedUser != null ? du.Document.CreatedUser.Fullname : string.Empty,
					CreatedAt = du.Document.CreatedAt,
					DocumentStatus = du.Document.Status,
					IsSequential = du.Document.IsSequential,
					UserSigningStatus = du.SigningStatus,
					AssignedAt = du.AssignedAt,
					Order = du.Order
				})
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<int> CountPendingSigningAssignmentsAsync(int userId)
		{
			using var context = new StorageContext();
			return await (
				from du in context.DocumentUsers
				join d in context.Documents on du.DocumentId equals d.Id
				where du.UserId == userId
					&& !d.IsDeleted
					&& (du.SigningStatus == SigningStatus.NOT_SIGNED
						|| du.SigningStatus == SigningStatus.PENDING)
				select du).CountAsync();
		}

		/// здесь ТОЛЬКО обновление статуса документа, НЕ статуса ДОКУМЕНТ-ЮЗЕР. 
		/// ОБНОВЛЕНИЕ СТАТУСА ДОКУМЕНТ-ЮЗЕР ВЫШЕ !!!
		/// Реальные Signature-записи создаёт SigningService после криптографического подписания.
		private async Task UpdateDocumentStatusAsync(StorageContext context, int documentId)
		{
			var document = await context.Documents.FirstOrDefaultAsync(x => x.Id == documentId);
			if (document == null || document.IsDeleted || document.Status == DocumentStatus.DECLINED)
			{
				return;
			}
			var documentUsers = await context.DocumentUsers
				.Where(x => x.DocumentId == documentId)
				.ToListAsync();
			if (documentUsers.Count == 0)
			{
				return;
			}
			if (documentUsers.Any(x => x.SigningStatus == SigningStatus.DECLINED))
			{
				document.Status = DocumentStatus.DECLINED;
				await context.SaveChangesAsync();
				return;
			}
			var statuses = documentUsers.Select(x => x.SigningStatus).ToList();
			DocumentStatus newStatus;
			if (statuses.All(x => x == SigningStatus.SIGNED))
			{
				newStatus = DocumentStatus.SIGNED;
			}
			else if (statuses.Any(x => x == SigningStatus.SIGNED || x == SigningStatus.PENDING))
			{
				newStatus = DocumentStatus.PARTLY_SIGNED;
			}
			else
			{
				newStatus = DocumentStatus.NOT_SIGNED;
			}
			if (document.Status != newStatus)
			{
				document.Status = newStatus;
				await context.SaveChangesAsync();
			}
		}
	}
}
