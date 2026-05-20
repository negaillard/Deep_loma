using Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public interface IDocumentUserModel : IId
	{
		int UserId { get; }
		int DocumentId { get; }
		SigningStatus SigningStatus { get; }
		DateTime? AssignedAt { get; }
		int Order { get; }
	}
}
