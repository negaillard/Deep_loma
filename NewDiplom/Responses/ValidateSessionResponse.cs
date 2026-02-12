using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Responses
{
	public class ValidateSessionResponse
	{
		public bool IsValid { get; set; }
		public string Login { get; set; }
	}
}
