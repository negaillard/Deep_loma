using Contracts.BindingModels;
using Contracts.ViewModels;
using Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Storage.Models
{
	public class DocumentUser
	{
		public int Id { get; private set; }
		[Required]
		public int UserId { get; set; }
		public virtual User User { get; set; }
		[Required]
		public int DocumentId { get; set; }
		public virtual Document Document { get; set; }
		[Required]
		public SigningStatus SigningStatus { get; set; }
		public DateTime? AssignedAt { get; set; }
		/// <summary>
		/// Порядковый номер подписи при последовательном режиме (1, 2, 3...).
		/// 0 означает что документ не последовательный.
		/// </summary>
		public int Order { get; set; }

		public static DocumentUser? Create(DocumentUserBindingModel model)
		{
			if (model == null)
			{
				return null;
			}
			return new DocumentUser
			{
				Id = model.Id,
				UserId = model.UserId,
				DocumentId = model.DocumentId,
				SigningStatus = model.SigningStatus,
				AssignedAt = model.AssignedAt,
				Order = model.Order,
			};
		}

		public void Update(DocumentUserBindingModel model)
		{
			if (model == null)
			{
				return;
			}
			SigningStatus = model.SigningStatus;
			AssignedAt = model.AssignedAt;
			Order = model.Order;
		}

		public DocumentUserViewModel GetViewModel => new()
		{
			Id = Id,
			UserId = UserId,
			DocumentId = DocumentId,
			SigningStatus = SigningStatus,
			AssignedAt = AssignedAt,
			Order = Order,
		};
	}
}
