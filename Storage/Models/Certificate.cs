using Contracts.BindingModels;
using Contracts.ViewModels;
using Models;
using System.ComponentModel.DataAnnotations;

namespace Storage.Models
{
	public class Certificate : ICertificateModel
	{
		public int Id { get; private set; }
		[Required] public DateTime StartDate { get; set; }
		[Required] public DateTime FinishDate { get; set; }
		[Required] public string PublicKey { get; set; } = string.Empty;
		[Required] public string Publisher { get; set; } = string.Empty;
		[Required] public string Owner { get; set; } = string.Empty;
		[Required] public string Number { get; set; } = string.Empty;
		[Required] public int UserId { get; set; }
		[Required] public bool IsActual { get; set; }
		[Required] public CertificateMode Mode { get; set; } = CertificateMode.Internal;
		public string FilePath { get; set; } = string.Empty;

		public static Certificate? Create(CertificateBindingModel model)
		{
			if (model == null)
			{
				return null;
			}
			return new Certificate
			{
				Id = model.Id,
				StartDate = model.StartDate,
				FinishDate = model.FinishDate,
				PublicKey = model.PublicKey,
				Publisher = model.Publisher,
				Owner = model.Owner,
				Number = model.Number,
				UserId = model.UserId,
				IsActual = model.IsActual,
				Mode = model.Mode,
				FilePath = model.FilePath,
			};
		}

		public void Update(CertificateBindingModel model)
		{
			if (model == null)
			{
				return;
			}
			StartDate = model.StartDate;
			FinishDate = model.FinishDate;
			PublicKey = model.PublicKey;
			Publisher = model.Publisher;
			Owner = model.Owner;
			Number = model.Number;
			UserId = model.UserId;
			IsActual = model.IsActual;
			Mode = model.Mode;
			FilePath = model.FilePath;
		}

		public CertificateViewModel GetViewModel => new()
		{
			Id = Id,
			StartDate = StartDate,
			FinishDate = FinishDate,
			PublicKey = PublicKey,
			Publisher = Publisher,
			Owner = Owner,
			Number = Number,
			UserId = UserId,
			IsActual = IsActual,
			Mode = Mode,
			FilePath = FilePath,
		};
	}
}

