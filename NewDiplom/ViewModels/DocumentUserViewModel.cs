using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.ViewModels
{
	public class DocumentUserViewModel : IDocumentUserModel
	{
		public int Id { get; set; }
		public int UserId { get; set; }
		public int DocumentId { get; set; }
		public SigningStatus SigningStatus { get; set; }
		public DateTime? AssignedAt { get; set; }
	}
}
