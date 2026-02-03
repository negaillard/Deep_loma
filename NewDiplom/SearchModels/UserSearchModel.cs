using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.SearchModels
{
	public class UserSearchModel
	{
		public string? Fullname { get; set; } 

		public string? Login { get; set; } 

		public string? Email { get; set; }

		public int? CertificateId { get; set; }

		public int? RoleId { get; set; }

		public int? Id { get; set; }

		public int? PageNumber { get; set; }

		public int? PageSize { get; set; }
	}
}
