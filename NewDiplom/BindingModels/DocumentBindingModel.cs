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
		public DocumentStatus Status { get; set; }

		public List<int> UserIds { get; set; } = new();
	}
}
