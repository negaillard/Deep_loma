using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public interface ICertificateModel
	{
		DateTime StartDate { get; }
		DateTime FinishDate { get; }
		string PublicKey { get; }
		string Publisher { get; }
		string Owner { get; }
		string Number { get; }
		int UserId {  get; }
	}
}
