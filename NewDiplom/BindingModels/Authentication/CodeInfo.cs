using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.BindingModels.Authentication
{
	public class CodeInfo
	{
		public string Code { get; set; }
		public string Email { get; set; }
		public DateTime CreatedAt { get; set; }
		public int Attempts { get; set; }
	}
}
