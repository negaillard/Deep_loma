using Models;

namespace Contracts.ViewModels
{
	public class CertificateViewModel
	{
		public int Id { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime FinishDate { get; set; }
		public string PublicKey { get; set; } = string.Empty;
		public string Publisher { get; set; } = string.Empty;
		public string Owner { get; set; } = string.Empty;
		public string Number { get; set; } = string.Empty;
		public int UserId { get; set; }
		public bool IsActual { get; set; }
		public CertificateMode Mode { get; set; }
		public string FilePath { get; set; } = string.Empty;
	}
}
