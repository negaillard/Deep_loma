using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.SearchModels
{
	public class SignatureSearchModel 
	{
		public string? SignatureValue { get; set; } = string.Empty;

		public int? CerificateId { get; set; }

		public DateTime? SignedAt { get; set; }

		public int? UserId { get; set; }

		public int? DocumentId { get; set; }

		public int? Id { get; set; }
		public bool? IsDeleted { get; set; }

		public int? PageNumber { get; set; }

		public int? PageSize { get; set; }
	}
}
