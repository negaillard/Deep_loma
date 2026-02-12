using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.SearchModels
{
	public class CertificateSearchModel
	{
		public int? Id { get; set; }
		public DateTime? StartDate { get; set; }

		public DateTime? FinishDate { get; set; }

		public string? PublicKey { get; set; } 

		public string? Publisher { get; set; }

		public string? Owner { get; set; }

		public string? Number { get; set; } 

		public int? UserId { get; set; }
		public bool? IsActual { get; set; }

		public int? PageNumber { get; set; }

		public int? PageSize { get; set; }
	}
}
