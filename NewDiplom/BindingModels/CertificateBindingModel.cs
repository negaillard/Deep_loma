using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.BindingModels
{
	public class CertificateBindingModel : ICertificateModel
	{
		public int Id { get; set; }
		public DateTime StartDate {  get; set; }

		public DateTime FinishDate { get; set; }

		public string PublicKey { get; set; } = string.Empty;

		public string Publisher { get; set; } = string.Empty;

		public string Owner { get; set; } = string.Empty;	

		public string Number { get; set; } = string.Empty;

		public int UserId { get; set; } 
		public bool IsActual { get; set; } = true;
	}
}
