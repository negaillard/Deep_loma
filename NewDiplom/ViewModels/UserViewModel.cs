using Contracts.StorageContracts;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.ViewModels
{
	public class UserViewModel : IUserModel
	{		
		public int Id { get; set; }
		public string Fullname { get; set; } = string.Empty;

		public string Login { get; set; } = string.Empty;

		public string Email { get; set; } = string.Empty;

		public int CertificateId { get; set; }

		public int RoleId { get; set; }
		public SystemRole SystemRole { get; set; }
		public DateTime Created { get; set; }

	}
}
