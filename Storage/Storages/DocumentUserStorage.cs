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
		/// здесь ТОЛЬКО обновление статуса документа, НЕ статуса ДОКУМЕНТ-ЮЗЕР. 
		/// ОБНОВЛЕНИЕ СТАТУСА ДОКУМЕНТ-ЮЗЕР ВЫШЕ !!!
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
			var signaturesAdded = false;
			if (statuses.All(x => x == SigningStatus.SIGNED))
			{
				newStatus = DocumentStatus.SIGNED;
				var existingSignatures = await context.Signatures
					.Where(x => x.DocumentId == documentId && !x.IsDeleted)
					.Select(x => x.Id)
					.ToListAsync();
				if (existingSignatures.Count == 0)
				{
					var userIds = documentUsers.Select(x => x.UserId).Distinct().ToList();
					var userCertificates = await context.Users
						.Where(x => userIds.Contains(x.Id))
						.Select(x => new { x.Id, x.CertificateId })
						.ToDictionaryAsync(x => x.Id, x => x.CertificateId);
					var signatures = userIds.Select(userId =>
					{
						var certificateId = userCertificates.TryGetValue(userId, out var value) ? value : 0;
						return new Signature
						{
							SignatureValue = string.Empty,
							CerificateId = certificateId,
							SignedAt = DateTime.UtcNow,
							UserId = userId,
							DocumentId = documentId,
							IsDeleted = false
						};
					}).ToList();
					await context.Signatures.AddRangeAsync(signatures);
					signaturesAdded = true;
				}
			}
			else if (statuses.Any(x => x == SigningStatus.SIGNED))
			{
				newStatus = DocumentStatus.PARTLY_SIGNED;
			}
			else
			{
				newStatus = DocumentStatus.NOT_SIGNED;
			}
			var statusChanged = document.Status != newStatus;
			if (statusChanged)
			{
				document.Status = newStatus;
			}
			if (statusChanged || signaturesAdded)
			{
				await context.SaveChangesAsync();
			}
		}
	}
}
