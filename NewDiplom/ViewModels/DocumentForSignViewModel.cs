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

	public class PagedResult<T>
	{
		public List<T> Items { get; set; } = [];
		public int TotalCount { get; set; }
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
		public bool HasPrevious => PageNumber > 1;
		public bool HasNext => PageNumber < TotalPages;
	}
}
