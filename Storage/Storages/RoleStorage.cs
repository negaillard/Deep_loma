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
		private readonly StorageContext _context;

		public RoleStorage(StorageContext context)
		{
			_context = context;
		}

		public async Task<RoleViewModel?> DeleteAsync(RoleBindingModel model)
		{
			var element = await _context.Roles.FirstOrDefaultAsync(rec => rec.Id == model.Id);
			if (element != null)
			{
				_context.Roles.Remove(element);
				await _context.SaveChangesAsync();
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
			var element = await _context.Roles
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
			return await _context.Roles
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
			return await _context.Roles
				.Where(x => x.Name.Contains(model.Name))
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<RoleViewModel>> GetFullListAsync()
		{
			return await _context.Roles
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
			return await _context.Roles
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
			await _context.Roles.AddAsync(newRole);
			await _context.SaveChangesAsync();
			return newRole.GetViewModel;
		}

		public async Task<RoleViewModel?> UpdateAsync(RoleBindingModel model)
		{
			var role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == model.Id);
			if (role == null)
			{
				return null;
			}
			role.Update(model);
			await _context.SaveChangesAsync();
			return role.GetViewModel;
		}
	}
}
