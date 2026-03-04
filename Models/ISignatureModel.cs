using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public interface ISignatureModel : IId
	{
		string SignatureValue { get; }
		int CerificateId { get; }
		DateTime SignedAt { get; }
		int UserId { get; }
		int DocumentId { get; }
		string Path { get; set; }
		bool IsDeleted { get; set; }
	}
}
