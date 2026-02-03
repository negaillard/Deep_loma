using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.BindingModels
{
	public class UserBindingModel : IUserModel
	{
		public string Fullname { get; set; } = string.Empty;

		public string Login { get; set; } = string.Empty;

		public string Email { get; set; } = string.Empty;

		public int CertificateId { get; set; } 

		public int RoleId {  get; set; }

		public int Id {  get; set; }
	}
}
