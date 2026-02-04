using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.BindingModels.Authentication
{
	public class EmailSettings
	{
		public string SmtpClientHost { get; set; } = "smtp.gmail.com";
		public int SmtpClientPort { get; set; } = 587;
		public string MailLogin { get; set; }
		public string MailPassword { get; set; }
		public string SenderName { get; set; } = "System";
		public bool EnableSsl { get; set; } = true;
	}
}
