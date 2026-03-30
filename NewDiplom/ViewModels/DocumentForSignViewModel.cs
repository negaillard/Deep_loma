using Models;

namespace Contracts.ViewModels
{
	public class DocumentForSignViewModel
	{
		public int Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string? Description { get; set; }
		public DateTime CreatedAt { get; set; }
		public DocumentStatus DocumentStatus { get; set; }
		public bool IsSequential { get; set; }
		public SigningStatus UserSigningStatus { get; set; }
		public DateTime? AssignedAt { get; set; }
		public int Order { get; set; }
	}
}
