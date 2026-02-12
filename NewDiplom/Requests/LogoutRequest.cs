using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Requests
{
	public class LogoutRequest
	{
		public string SessionToken { get; set; }
	}
}
