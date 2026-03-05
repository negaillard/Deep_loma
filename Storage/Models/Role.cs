using Contracts.BindingModels;
using Contracts.ViewModels;
using Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Models
{
	public class Role : IRoleModel
	{
		public int Id {  get; private set; }

		[Required]
		public string Name { get; set; } = string.Empty;

		[Required]
		public string Description { get; set; } = string.Empty;

		[ForeignKey("RoleId")]
		public virtual List<User> Users { get; set; } = new(); // опасное место

		public static Role? Create(RoleBindingModel model)
		{
			if (model == null)
			{
				return null;
			}
			return new Role()
			{
				Id = model.Id,
				Name = model.Name,
				Description = model.Description,
			};
		}
		public static Role Create(RoleViewModel model)
		{
			return new Role
			{
				Id = model.Id,
				Name = model.Name,
				Description = model.Description,	
			};
		}
		public void Update(RoleBindingModel model)
		{
			if (model == null)
			{
				return;
			}
			Name = model.Name;
			Description = model.Description;
		}
		public RoleViewModel GetViewModel => new()
		{
			Id = Id,
			Name = Name,
			Description = Description,
		};
	}
}
