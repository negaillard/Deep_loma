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
	public class UserStorage : IUserStorage
	{
		private readonly StorageContext _context;

		public UserStorage(StorageContext context)
		{
			_context = context;
		}

		public async Task<UserViewModel?> DeleteAsync(UserBindingModel model)
		{
			var element = await _context.Users.FirstOrDefaultAsync(rec => rec.Id == model.Id);
			if (element != null)
			{
				if (!element.IsActive)
				{
					return element.GetViewModel;
				}
				element.IsActive = false;
				await _context.SaveChangesAsync();
				return element.GetViewModel;
			}
			return null;
		}

		public async Task<UserViewModel?> GetElementAsync(UserSearchModel model)
		{
			if (string.IsNullOrEmpty(model.Login) && !model.Id.HasValue)
			{
				return null;
			}
			var query = _context.Users.AsQueryable();
			if (model.IsActive.HasValue)
			{
				query = query.Where(x => x.IsActive == model.IsActive.Value);
			}
			var element = await query.FirstOrDefaultAsync(x =>
				(!string.IsNullOrEmpty(model.Login) && x.Login == model.Login) ||
				(model.Id.HasValue && x.Id == model.Id));
			if (element != null) {
				return element.GetViewModel;
			}	
			return null;
		}

		public async Task<List<UserViewModel>> GetFilteredListAsync(UserSearchModel model)
		{
			if (string.IsNullOrEmpty(model.Login) && !model.RoleId.HasValue)
			{
				return new();
			}
			var query = _context.Users.AsQueryable();
			return await query
				.Where(x =>
				(!string.IsNullOrEmpty(model.Login) && x.Login == model.Login) || (model.RoleId.HasValue && x.RoleId == model.RoleId))
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<UserViewModel>> GetFilteredListByFullnameContainsAsync(UserSearchModel model)
		{
			if (string.IsNullOrEmpty(model.Fullname))
			{
				return new();
			}
			var query = _context.Users.AsQueryable();
			if (model.IsActive.HasValue)
			{
				query = query.Where(x => x.IsActive == model.IsActive.Value);
			}
			return await query
				.Where(x => x.Fullname.Contains(model.Fullname))
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<UserViewModel>> GetFullListAsync()
		{
			return await _context.Users
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<UserViewModel>> GetPagedListAsync(UserSearchModel model)
		{
			if (!model.PageNumber.HasValue || !model.PageSize.HasValue || model.PageNumber < 1 || model.PageSize < 1)
			{
				return new();
			}
			var skip = (model.PageNumber.Value - 1) * model.PageSize.Value;
			var query = _context.Users.AsQueryable();
			if (model.IsActive.HasValue)
			{
				query = query.Where(x => x.IsActive == model.IsActive.Value);
			}
			return await query
				.OrderBy(x => x.Id)
				.Skip(skip)
				.Take(model.PageSize.Value)
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<UserViewModel?> InsertAsync(UserBindingModel model)
		{
			var newUser = User.Create(model);
			if (newUser == null)
			{
				return null;
			}
			newUser.IsActive = model.IsActive;
			await _context.Users.AddAsync(newUser);
			await _context.SaveChangesAsync();
			return newUser.GetViewModel;
		}

		public async Task<UserViewModel?> UpdateAsync(UserBindingModel model)
		{
			var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == model.Id);
			if (user == null)
			{
				return null;
			}
			user.Update(model);
			await _context.SaveChangesAsync();
			return user.GetViewModel;
		}
	}
}
