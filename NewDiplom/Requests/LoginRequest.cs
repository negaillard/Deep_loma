using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Requests
{
	public class LoginRequest
	{
		public string Login {  get; set; }
		public AppType appType { get; set; }
	}
}
