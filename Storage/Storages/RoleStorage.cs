using Contracts.BindingModels;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using Microsoft.EntityFrameworkCore;
using Storage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Storages
{
	public class RoleStorage : IRoleStorage
	{
		public async Task<RoleViewModel?> DeleteAsync(RoleBindingModel model)
		{
			using var context = new StorageContext();
			var element = await context.Roles.FirstOrDefaultAsync(rec => rec.Id == model.Id);
			if (element != null)
			{
				context.Roles.Remove(element);
				await context.SaveChangesAsync();
				return element.GetViewModel;
			}
			return null;
		}

		public async Task<RoleViewModel?> GetElementAsync(RoleSearchModel model)
		{
			if (string.IsNullOrEmpty(model.Name) && !model.Id.HasValue)
			{
				return null;
			}
			using var context = new StorageContext();
			var element = await context.Roles
				.FirstOrDefaultAsync(x =>
					(!string.IsNullOrEmpty(model.Name) && x.Name == model.Name) ||
					(model.Id.HasValue && x.Id == model.Id));
			if (element != null)
			{
				return element.GetViewModel;
			}
			return null;
		}

		public async Task<List<RoleViewModel>> GetFilteredListAsync(RoleSearchModel model)
		{
			if (string.IsNullOrEmpty(model.Name))
			{
				return new();
			}
			using var context = new StorageContext();
			return await context.Roles
				.Where(x => x.Name.Contains(model.Name))
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<RoleViewModel>> GetFilteredListByNameContainsAsync(RoleSearchModel model)
		{
			if (string.IsNullOrEmpty(model.Name))
			{
				return new();
			}
			using var context = new StorageContext();
			return await context.Roles
				.Where(x => x.Name.Contains(model.Name))
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<RoleViewModel>> GetFullListAsync()
		{
			using var context = new StorageContext();
			return await context.Roles
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<RoleViewModel>> GetPagedListAsync(RoleSearchModel model)
		{
			if (!model.PageNumber.HasValue || !model.PageSize.HasValue || model.PageNumber < 1 || model.PageSize < 1)
			{
				return new();
			}
			var skip = (model.PageNumber.Value - 1) * model.PageSize.Value;
			using var context = new StorageContext();
			return await context.Roles
				.OrderBy(x => x.Id)
				.Skip(skip)
				.Take(model.PageSize.Value)
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<RoleViewModel?> InsertAsync(RoleBindingModel model)
		{
			var newRole = Role.Create(model);
			if (newRole == null)
			{
				return null;
			}
			using var context = new StorageContext();
			await context.Roles.AddAsync(newRole);
			await context.SaveChangesAsync();
			return newRole.GetViewModel;
		}

		public async Task<RoleViewModel?> UpdateAsync(RoleBindingModel model)
		{
			using var context = new StorageContext();
			var role = await context.Roles.FirstOrDefaultAsync(x => x.Id == model.Id);
			if (role == null)
			{
				return null;
			}
			role.Update(model);
			await context.SaveChangesAsync();
			return role.GetViewModel;
		}
	}
}

