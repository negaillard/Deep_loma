using Contracts.LogicContracts;
using Contracts.ViewModels;
using CryptoPro.Security.Cryptography.Pkcs;
using CryptoPro.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace SigningService.Signing
{
	/// <summary>
	/// Реализует подписание документа в юридически значимом режиме.
	/// Алгоритм: ГОСТ Р 34.10-2012 / ГОСТ Р 34.11-2012 через КриптоПро CSP.
	///
	/// Сертификат должен быть установлен в хранилище Windows (certmgr.msc → Личные).
	/// Поле Certificate.Number — серийный номер сертификата, по которому ведётся поиск.
	/// </summary>
	public class CryptoProDocumentSigner : IDocumentSigner
	{
		private readonly ILogger<CryptoProDocumentSigner> _logger;

		public CryptoProDocumentSigner(ILogger<CryptoProDocumentSigner> logger)
		{
			_logger = logger;
		}

		public Task<byte[]> SignAsync(byte[] documentBytes, CertificateViewModel certificate)
		{
			_logger.LogInformation(
				"Поиск ГОСТ-сертификата в хранилище. SerialNumber={Number}", certificate.Number);

			var gostCert = FindCertificate(certificate.Number)
				?? throw new InvalidOperationException(
					$"Сертификат с серийным номером '{certificate.Number}' не найден " +
					"ни в LocalMachine\\My, ни в CurrentUser\\My. " +
					"Установите сертификат через certmgr.msc.");

			using (gostCert)
			{
				var contentInfo = new ContentInfo(documentBytes);
				var signedCms   = new CpSignedCms(contentInfo, detached: true);
				var cmsSigner   = new CpCmsSigner(gostCert);

				cmsSigner.SignedAttributes.Add(new Pkcs9SigningTime(DateTime.UtcNow));

				signedCms.ComputeSignature(cmsSigner);
				var result = signedCms.Encode();

				_logger.LogInformation(
					"Документ подписан ГОСТ-подписью. SignatureLength={Len}", result.Length);

				return Task.FromResult(result);
			}
		}

		/// <summary>
		/// Ищет сертификат сначала в LocalMachine\My, затем в CurrentUser\My.
		/// </summary>
		private static CpX509Certificate2? FindCertificate(string serialNumber)
		{
			foreach (var location in new[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser })
			{
				using var store = new CpX509Store(StoreName.My, location);
				store.Open(OpenFlags.ReadOnly);

				var found = store.Certificates.Find(
					X509FindType.FindBySerialNumber, serialNumber, validOnly: false);

				if (found.Count > 0)
					return found[0];
			}

			return null;
		}
	}
}
