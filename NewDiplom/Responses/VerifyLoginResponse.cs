using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Responses
{
	public class VerifyLoginResponse
	{
		public string Message { get; set; }
		public string Login { get; set; }
		public int UserId { get; set; }
		public string SessionToken { get; set; }
		public SystemRole SystemRole { get; set; }
	}
}
