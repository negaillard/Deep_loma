using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using Microsoft.EntityFrameworkCore;
using Storage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Storage.Storages
{
	public class CertificateStorage : ICertificateStorage
	{
		public async Task<CertificateViewModel?> DeleteAsync(CertificateBindingModel model)
		{
			using var context = new StorageContext();
			var element = await FindElementAsync(context, model);
			if (element != null)
			{
				context.Certificates.Remove(element);
				await context.SaveChangesAsync();
				return element.GetViewModel;
			}
			return null;
		}

		public async Task<CertificateViewModel?> GetElementAsync(CertificateSearchModel model)
		{
			if (!model.Id.HasValue &&
				string.IsNullOrEmpty(model.Number) &&
				!model.UserId.HasValue)
			{
				return null;
			}
			using var context = new StorageContext();
			var query = context.Certificates.AsQueryable();
			if (model.IsActual.HasValue)
			{
				query = query.Where(x => x.IsActual == model.IsActual.Value);
			}
			else if (model.UserId.HasValue)
			{
				query = query.Where(x => x.IsActual);
			}
			var element = await query.FirstOrDefaultAsync(x =>
				(model.Id.HasValue && x.Id == model.Id) ||
				(!string.IsNullOrEmpty(model.Number) && x.Number == model.Number) ||
				(model.UserId.HasValue && x.UserId == model.UserId));
			if (element != null)
			{
				return element.GetViewModel;
			}
			return null;
		}

		public async Task<List<CertificateViewModel>> GetFilteredListAsync(CertificateSearchModel model)
		{
			if (!model.Id.HasValue &&
				!model.StartDate.HasValue &&
				!model.FinishDate.HasValue &&
				string.IsNullOrEmpty(model.PublicKey) &&
				string.IsNullOrEmpty(model.Publisher) &&
				string.IsNullOrEmpty(model.Owner) &&
				string.IsNullOrEmpty(model.Number) &&
				!model.UserId.HasValue &&
				!model.IsActual.HasValue)
			{
				return new();
			}
			using var context = new StorageContext();
			var query = context.Certificates.AsQueryable();
			if (model.Id.HasValue)
			{
				query = query.Where(x => x.Id == model.Id);
			}
			if (model.StartDate.HasValue)
			{
				query = query.Where(x => x.StartDate == model.StartDate.Value);
			}
			if (model.FinishDate.HasValue)
			{
				query = query.Where(x => x.FinishDate == model.FinishDate.Value);
			}
			if (!string.IsNullOrEmpty(model.PublicKey))
			{
				query = query.Where(x => x.PublicKey.Contains(model.PublicKey));
			}
			if (!string.IsNullOrEmpty(model.Publisher))
			{
				query = query.Where(x => x.Publisher.Contains(model.Publisher));
			}
			if (!string.IsNullOrEmpty(model.Owner))
			{
				query = query.Where(x => x.Owner.Contains(model.Owner));
			}
			if (!string.IsNullOrEmpty(model.Number))
			{
				query = query.Where(x => x.Number.Contains(model.Number));
			}
			if (model.UserId.HasValue)
			{
				query = query.Where(x => x.UserId == model.UserId);
			}
			if (model.IsActual.HasValue)
			{
				query = query.Where(x => x.IsActual == model.IsActual.Value);
			}
			return await query
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<CertificateViewModel>> GetFullListAsync()
		{
			using var context = new StorageContext();
			return await context.Certificates
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<CertificateViewModel>> GetPagedListAsync(CertificateSearchModel model)
		{
			if (!model.PageNumber.HasValue || !model.PageSize.HasValue || model.PageNumber < 1 || model.PageSize < 1)
			{
				return new();
			}
			var skip = (model.PageNumber.Value - 1) * model.PageSize.Value;
			using var context = new StorageContext();
			return await context.Certificates
				.OrderBy(x => x.Id)
				.Skip(skip)
				.Take(model.PageSize.Value)
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<CertificateViewModel?> InsertAsync(CertificateBindingModel model)
		{
			var newCertificate = Certificate.Create(model);
			if (newCertificate == null)
			{
				return null;
			}
			using var context = new StorageContext();
			var user = await context.Users.FirstOrDefaultAsync(x => x.Id == model.UserId);
			if (user == null)
			{
				throw new InvalidOperationException("Пользователь не найден");
			}
			var oldCertificates = await context.Certificates
				.Where(x => x.UserId == model.UserId && x.IsActual)
				.ToListAsync();
			foreach (var certificate in oldCertificates)
			{
				certificate.IsActual = false;
			}
			newCertificate.IsActual = true;
			await context.Certificates.AddAsync(newCertificate);
			await context.SaveChangesAsync();
			user.CertificateId = newCertificate.Id;
			await context.SaveChangesAsync();
			return newCertificate.GetViewModel;
		}

		public async Task<CertificateViewModel?> UpdateAsync(CertificateBindingModel model)
		{
			using var context = new StorageContext();
			var certificate = await FindElementAsync(context, model);
			if (certificate == null)
			{
				return null;
			}
			if (model.IsActual)
			{
				var user = await context.Users.FirstOrDefaultAsync(x => x.Id == model.UserId);
				if (user == null)
				{
					throw new InvalidOperationException("Пользователь не найден");
				}
				var oldCertificates = await context.Certificates
					.Where(x => x.UserId == model.UserId && x.IsActual && x.Id != certificate.Id)
					.ToListAsync();
				foreach (var oldCertificate in oldCertificates)
				{
					oldCertificate.IsActual = false;
				}
				user.CertificateId = certificate.Id;
			}
			certificate.Update(model);
			await context.SaveChangesAsync();
			return certificate.GetViewModel;
		}

		private static Task<Certificate?> FindElementAsync(StorageContext context, CertificateBindingModel model)
		{
			if (model.Id > 0)
			{
				return context.Certificates.FirstOrDefaultAsync(x => x.Id == model.Id);
			}
			if (!string.IsNullOrEmpty(model.Number))
			{
				if (model.UserId > 0)
				{
					return context.Certificates.FirstOrDefaultAsync(x => x.Number == model.Number && x.UserId == model.UserId);
				}
				return context.Certificates.FirstOrDefaultAsync(x => x.Number == model.Number);
			}
			if (model.UserId > 0)
			{
				return context.Certificates.FirstOrDefaultAsync(x => x.UserId == model.UserId);
			}
			return Task.FromResult<Certificate?>(null);
		}
	}
}

