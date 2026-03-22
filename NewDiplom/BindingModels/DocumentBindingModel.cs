using Models;

namespace Contracts.BindingModels
{
	public class DocumentBindingModel : IDocumentModel
	{
		public int Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
		public int CreatedByUserId { get; set; }
		public string Path { get; set; } = string.Empty;
		public DocumentStatus Status { get; set; }
		public bool IsDeleted { get; set; } = false;
		public bool IsSequential { get; set; } = false;

		public List<int> UserIds { get; set; } = new();
	}
}
