using Models.Enums;
using System.Collections.Generic;

namespace Contracts.SearchModels
{
	public class DocumentSearchModel
	{
		public int? Id { get; set; }
		public string? Title { get; set; }

		public string? Description { get; set; }

		/// <summary>Поиск по вхождению в название или описание (одно поле).</summary>
		public string? SearchText { get; set; }

		public DateTime? CreatedAt { get; set; }

		public int? CreatedByUserId { get; set; }

		public DocumentStatus? Status { get; set; }

		/// <summary>Фильтр по нескольким статусам (имеет приоритет над <see cref="Status"/>, если задано).</summary>
		public List<DocumentStatus>? Statuses { get; set; }

		public bool? IsDeleted { get; set; }

		public int? PageNumber { get; set; }

		public int? PageSize { get; set; }
	}
}
