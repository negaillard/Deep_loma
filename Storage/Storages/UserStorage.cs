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
		public async Task<UserViewModel?> DeleteAsync(UserBindingModel model)
		{
			using var context = new StorageContext();
			var element = await context.Users.FirstOrDefaultAsync(rec => rec.Id == model.Id);
			if (element != null)
			{
				context.Users.Remove(element);
				await context.SaveChangesAsync();
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
			using var context = new StorageContext();
			var element = await context.Users
				.FirstOrDefaultAsync(x =>
					(!string.IsNullOrEmpty(model.Login) && x.Login == model.Login) ||
					(model.Id.HasValue && x.Id == model.Id));
			if (element != null) {
				return element.GetViewModel;
			}	
			return null;
		}

		public async Task<List<UserViewModel>> GetFilteredListAsync(UserSearchModel model)
		{
			if (string.IsNullOrEmpty(model.Login))
			{
				return new();
			}
			using var context = new StorageContext();
			return await context.Users
				.Where(x => x.Login.Contains(model.Login))
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<UserViewModel>> GetFilteredListByFullnameContainsAsync(UserSearchModel model)
		{
			if (string.IsNullOrEmpty(model.Fullname))
			{
				return new();
			}
			using var context = new StorageContext();
			return await context.Users
				.Where(x => x.Fullname.Contains(model.Fullname))
				.Select(x => x.GetViewModel)
				.ToListAsync();
		}

		public async Task<List<UserViewModel>> GetFullListAsync()
		{
			using var context = new StorageContext();
			return await context.Users
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
			using var context = new StorageContext();
			return await context.Users
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
			using var context = new StorageContext();
			await context.Users.AddAsync(newUser);
			await context.SaveChangesAsync();
			return newUser.GetViewModel;
		}

		public async Task<UserViewModel?> UpdateAsync(UserBindingModel model)
		{
			using var context = new StorageContext();
			var user = await context.Users.FirstOrDefaultAsync(x => x.Id == model.Id);
			if (user == null)
			{
				return null;
			}
			user.Update(model);
			await context.SaveChangesAsync();
			return user.GetViewModel;
		}
	}
}
