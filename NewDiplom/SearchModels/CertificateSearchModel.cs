using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.SearchModels
{
	public class CertificateSearchModel
	{
		public DateTime? StartDate { get; set; }

		public DateTime? FinishDate { get; set; }

		public string? PublicKey { get; set; } 

		public string? Publisher { get; set; }

		public string? Owner { get; set; }

		public string? Number { get; set; } 

		public int? UserId { get; set; }
	}
}
