using Models;

namespace Contracts.BindingModels
{
	public class DocumentUserBindingModel : IDocumentUserModel
	{
		public int Id { get; set; }
		public int UserId { get; set; }
		public int DocumentId { get; set; }
		public SigningStatus SigningStatus { get; set; }
		public DateTime? AssignedAt { get; set; }
	}
}






