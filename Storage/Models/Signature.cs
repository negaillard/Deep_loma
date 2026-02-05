using Contracts.BindingModels;
using Contracts.ViewModels;
using Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace Storage.Models
{
	public class Signature : ISignatureModel
	{
		public int Id { get; private set; }
		[Required]
		public string SignatureValue { get; set; } = string.Empty;
		[Required]
		public int CerificateId { get; set; }
		[Required]
		public DateTime SignedAt { get; set; }
		[Required]
		public int UserId { get; set; }
		[Required]
		public int DocumentId { get; set; }

		public static Signature? Create(SignatureBindingModel model)
		{
			if (model == null)
			{
				return null;
			}
			return new Signature
			{
				Id = model.Id,
				SignatureValue = model.SignatureValue,
				CerificateId = model.CerificateId,
				SignedAt = model.SignedAt,
				UserId = model.UserId,
				DocumentId = model.DocumentId,
			};
		}

		public void Update(SignatureBindingModel model)
		{
			if (model == null)
			{
				return;
			}
			SignatureValue = model.SignatureValue;
			CerificateId = model.CerificateId;
			SignedAt = model.SignedAt;
			UserId = model.UserId;
			DocumentId = model.DocumentId;
		}

		public SignatureViewModel GetViewModel => new()
		{
			Id = Id,
			SignatureValue = SignatureValue,
			CerificateId = CerificateId,
			SignedAt = SignedAt,
			UserId = UserId,
			DocumentId = DocumentId,
		};
	}
}


