using Contracts.BindingModels;
using Contracts.ViewModels;
using Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Models
{
	public class Document : IDocumentModel
	{
		public int Id {  get; private set; }
		[Required]
		public string Title { get; set; } = string.Empty;
		[Required]
		public string Description { get; set; } = string.Empty;
		public string Path { get; set; } = string.Empty;
		[Required]
		public DateTime CreatedAt { get; set; }
		[Required]
		public DocumentStatus Status { get; set; }
		[Required]
		public bool IsDeleted { get; set; }
		[Required]
		public int CreatedByUserId { get; set; } 
		public virtual User CreatedUser { get; set; }

		[ForeignKey("DocumentId")]
		public virtual List<DocumentUser> DocumentUsers { get; set; } = new();

		public static Document? Create(DocumentBindingModel model)
		{
			if (model == null)
			{
				return null;
			}
			return new Document()
			{
				Id = model.Id,
				Title = model.Title,
				Description = model.Description,
				Path = model.Path,
				CreatedAt = model.CreatedAt,
				CreatedByUserId = model.CreatedByUserId,
				Status = model.Status,
				IsDeleted = model.IsDeleted,
			};
		}
		public static Document Create(DocumentViewModel model)
		{
			return new Document
			{
				Id = model.Id,
				Title = model.Title,
				Description = model.Description,
				Path = model.Path,
				CreatedAt = model.CreatedAt,
				CreatedByUserId = model.CreatedByUserId,
				Status = model.Status,
				IsDeleted = model.IsDeleted,
			};
		}
		public void Update(DocumentBindingModel model)
		{
			if (model == null)
			{
				return;
			}
			Title = model.Title;
			Description = model.Description;
			Path = model.Path;
			Status = model.Status;
			IsDeleted = model.IsDeleted;
		}
		public DocumentViewModel GetViewModel => new()
		{
			Id = Id,
			Title = Title,
			Description = Description,
			Path = Path,
			CreatedAt = CreatedAt,
			CreatedByUserId = CreatedByUserId,
			Status = Status,
			IsDeleted = IsDeleted,
		};
	}
}
