using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public interface IDocumentModel : IId
	{
		string Title { get; }
		string Description { get; }
		DateTime CreatedAt { get; }
		int CreatedByUserId {get; }
		string Path { get; set; }
		DocumentStatus Status { get; set;}
		bool IsDeleted { get; set; }

	}
}
