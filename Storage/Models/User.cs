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
using System.Xml.Linq;

namespace Storage.Models
{
	public class User : IUserModel
	{
		public int Id {get; private set;}
		[Required]
		public string Fullname {get; set;} = string.Empty;
		[Required]
		public string Login { get; set; } = string.Empty;
		[Required]
		public string Email { get; set; } = string.Empty;
		[Required]
		public int CertificateId { get; set; }
		[Required]
		public int RoleId { get; set; }
		public virtual Role Role { get; private set; }
		[Required]
		public SystemRole SystemRole { get; set; }
		[Required]
		public DateTime Created { get; set; }
		[Required]
		public bool IsActive { get; set; }

		[ForeignKey("CreatedByUserId")]
		public virtual List<Document> Documents { get; set; } = new();

		[ForeignKey("UserId")]
		public virtual List<DocumentUser> DocumentUsers { get; set; } = new();

		public static User? Create(UserBindingModel model)
		{
			if (model == null)
			{
				return null;
			}
			return new User()
			{
				Id = model.Id,
				Fullname = model.Fullname,
				Login = model.Login,
				Email = model.Email,
				CertificateId = model.CertificateId,
				RoleId = model.RoleId,
				SystemRole = model.SystemRole,
				Created = model.Created,
				IsActive = model.IsActive,
			};
		}
		public static User Create(UserViewModel model)
		{
			return new User
			{
				Id = model.Id,
				Fullname = model.Fullname,
				Login = model.Login,
				Email = model.Email,
				CertificateId = model.CertificateId,
				RoleId = model.RoleId,
				SystemRole = model.SystemRole,
				Created = model.Created,
				IsActive = model.IsActive,
			};
		}
		public void Update(UserBindingModel model)
		{
			if (model == null)
			{
				return;
			}
			CertificateId = model.CertificateId;
			IsActive = model.IsActive;
			Login = model.Login;
			Email = model.Email;
			SystemRole = model.SystemRole;
			RoleId = model.RoleId;
			Fullname = model.Fullname;
		}
		public UserViewModel GetViewModel => new()
		{
			Id = Id,
			Fullname = Fullname,
			Login = Login,
			Email = Email,
			CertificateId = CertificateId,
			RoleId = RoleId,
			SystemRole = SystemRole,
			Created = Created,
			IsActive = IsActive,
		};
	}
}
