using Contracts.LogicContracts;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace SigningService.Signing
{
	/// <summary>
	/// Реализует подписание документа для внутреннего режима.
	/// Алгоритм: RSA-2048 / SHA-256 / PKCS#7 CMS detached.
	/// Закрытый ключ извлекается из PFX-файла, хранящегося в файловом хранилище.
	/// PFX сохранён без пароля (см. SelfSignedCertificateGenerator).
	/// </summary>
	public class InternalDocumentSigner : IDocumentSigner
	{
		private readonly IFileStorage _fileStorage;

		public InternalDocumentSigner(IFileStorage fileStorage)
		{
			_fileStorage = fileStorage;
		}

		public async Task<byte[]> SignAsync(byte[] documentBytes, CertificateViewModel certificate)
		{
			var pfxBytes = await _fileStorage.GetCertificateBytesAsync(certificate.FilePath);

			using var x509 = new X509Certificate2(
				pfxBytes,
				(string?)null,
				X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);

			var contentInfo = new ContentInfo(documentBytes);

			// detached: подпись хранится отдельно от документа
			var signedCms = new SignedCms(contentInfo, detached: true);

			var cmsSigner = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, x509)
			{
				DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1"), // SHA-256
				SignedAttributes =
				{
					new Pkcs9SigningTime(DateTime.UtcNow)
				}
			};

			signedCms.ComputeSignature(cmsSigner);

			return signedCms.Encode();
		}
	}
}
