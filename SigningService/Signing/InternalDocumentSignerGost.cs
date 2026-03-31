using Contracts.LogicContracts;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System.Linq;

namespace SigningService.Signing
{
	/// <summary>
	/// Подписание документа для внутреннего режима: ГОСТ Р 34.11-2012 + ГОСТ Р 34.10-2012, PKCS#7 CMS detached (BouncyCastle).
	/// </summary>
	public class InternalDocumentSignerGost : IDocumentSigner
	{
		/// <summary>Та же связка алгоритмов, что при выпуске сертификата в SelfSignedCertificateGeneratorGost.</summary>
		private const string GostSignatureAlgorithm = "GOST3411-2012-256WITHECGOST3410-2012-256";

		/// <summary>Доступ к PFX на диске.</summary>
		private readonly IFileStorage _fileStorage;

		public InternalDocumentSignerGost(IFileStorage fileStorage)
		{
			_fileStorage = fileStorage; // сохраняем файловое хранилище для чтения .pfx
		}

		/// <inheritdoc />
		public async Task<byte[]> SignAsync(byte[] documentBytes, CertificateViewModel certificate)
		{
			var pfxBytes = await _fileStorage.GetCertificateBytesAsync(certificate.FilePath); // читаем контейнер PKCS#12 по пути из БД
			using var pfxStream = new MemoryStream(pfxBytes); // поток для загрузки в BouncyCastle
			var store = new Pkcs12StoreBuilder().Build(); // создаём пустое PKCS#12-хранилище
			store.Load(pfxStream, Array.Empty<char>()); // загружаем PFX без пароля (как при генерации)

			var alias = store.Aliases.Cast<string>().FirstOrDefault(store.IsKeyEntry); // ищем первый алиас с закрытым ключом
			if (alias == null) // без ключа подписать нельзя
				throw new InvalidOperationException("В PKCS#12 не найдена запись с закрытым ключом.");

			var privKey = store.GetKey(alias).Key; // закрытый ключ ГОСТ из контейнера
			var bcCert = store.GetCertificate(alias).Certificate; // сертификат подписанта (Org.BouncyCastle.X509)

			var random = new SecureRandom(); // случайность для подписи CMS (если требуется провайдеру)
			var sigFactory = new Asn1SignatureFactory(GostSignatureAlgorithm, privKey, random); // фабрика подписи по ГОСТ 2012
			var signerInfoGen = new SignerInfoGeneratorBuilder().Build(sigFactory, bcCert); // генератор SignerInfo для CMS

			var cmsGen = new CmsSignedDataGenerator(); // генератор SignedData (PKCS#7)
			cmsGen.AddSignerInfoGenerator(signerInfoGen); // добавляем одного подписанта

			var content = new CmsProcessableByteArray(documentBytes); // оборачиваем байты документа как содержимое CMS
			var signedData = cmsGen.Generate(content, encapsulate: false); // detached: тело документа не внутри SignedData
			return signedData.GetEncoded(); // DER PKCS#7 для сохранения как .sig
		}
	}
}
