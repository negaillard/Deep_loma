using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.BindingModels.Authentication
{
	public class UserSession
	{
		public string SessionId { get; set; }
		public int UserId { get; set; }
		public string Username { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime ExpiresAt { get; set; }
		public bool IsActive { get; set; }
	}
}
