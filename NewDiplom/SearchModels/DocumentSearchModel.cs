using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.SearchModels
{
	public class DocumentSearchModel
	{
		public string? Title { get; set; } 

		public string? Description { get; set; } 

		public DateTime? CreatedAt { get; set; }

		public int? CreatedByUserId { get; set; }

		public int? Id { get; set; }
	}
}
