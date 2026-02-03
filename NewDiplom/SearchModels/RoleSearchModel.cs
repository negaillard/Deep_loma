using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.SearchModels
{
	public class RoleSearchModel
	{
		public string? Name { get; set; } 

		public string? Description { get; set; } 

		public int? Id { get; set; }

		public int? PageNumber { get; set; }

		public int? PageSize { get; set; }
	}
}
