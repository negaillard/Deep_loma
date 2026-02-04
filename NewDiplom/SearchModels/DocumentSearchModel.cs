using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.SearchModels
{
	public class DocumentSearchModel
	{
		public int? Id { get; set; }
		public string? Title { get; set; } 

		public string? Description { get; set; } 

		public DateTime? CreatedAt { get; set; }

		public int? CreatedByUserId { get; set; }

		public DocumentStatus? Status { get; set; }

		public int? PageNumber { get; set; }

		public int? PageSize { get; set; }
	}
}
