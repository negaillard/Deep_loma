using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.StorageContracts;
using Models;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Logic
{
	/// <summary>
	/// Генерирует самоподписанные сертификаты для внутреннего режима работы системы.
	/// Алгоритм: RSA-2048 / SHA-256. Срок действия: 1 год.
	/// Файл ключевой пары сохраняется как PKCS#12 (.pfx) на файловый сервер.
	/// Публичный сертификат хранится в БД в PEM-формате (поле PublicKey).
	/// </summary>
	public class SelfSignedCertificateGenerator : ICertificateGeneratorLogic
	{
		private readonly IFileStorage _fileStorage;

		public SelfSignedCertificateGenerator(IFileStorage fileStorage)
		{
			_fileStorage = fileStorage;
		}

		public async Task<CertificateBindingModel> GenerateSelfSignedAsync(int userId, string owner, string publisher)
		{
			if (userId <= 0)
				throw new ArgumentException("Не указан идентификатор пользователя", nameof(userId));
			if (string.IsNullOrWhiteSpace(owner))
				throw new ArgumentException("Не указан владелец сертификата", nameof(owner));
			if (string.IsNullOrWhiteSpace(publisher))
				throw new ArgumentException("Не указан издатель сертификата", nameof(publisher));

			using var rsa = RSA.Create(2048);

			// стандартный формат идентификатора в Х509 - имя сертификата содержит информацию о владельце и издателе
			var distinguishedName = new X500DistinguishedName(
				$"CN={EscapeRdnValue(owner)}, O={EscapeRdnValue(publisher)}");

			// заготовка для сертификата
			var request = new CertificateRequest(
				distinguishedName,
				rsa,
				HashAlgorithmName.SHA256,
				RSASignaturePadding.Pkcs1);

			// Расширение Basic Constraints говорит, является ли этот сертификат удостоверяющим центром (CA). НЕТ
			request.CertificateExtensions.Add(
				new X509BasicConstraintsExtension(false, false, 0, false));

			// описываем для чего может использоваться сертификат
			request.CertificateExtensions.Add(
				new X509KeyUsageExtension(
					X509KeyUsageFlags.DigitalSignature // для ЭП
					| X509KeyUsageFlags.NonRepudiation, // подписант не может отрицать факт подписания
					critical: true)); // любое ПО, которое не понимает это расширение, обязано отклонить сертификат

			// добавляем в сертификат уникальный отпечаток открытого ключа, для быстрого поиска по этому отпечатку
			request.CertificateExtensions.Add(
				new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));

			// секртификат действителен в течение одного года
			var startDate = DateTimeOffset.UtcNow;
			var endDate = startDate.AddYears(1);

			// генерация случайного серийного нномера для сохранения в бд
			var serialNumber = GenerateSerialNumber();

			// подписывает заготовку тем же ключом, который в ней содержится (самоподписание).
			// Возвращает готовый X509Certificate2 с встроенным закрытым ключом
			using var certificate = request.CreateSelfSigned(startDate, endDate);

			// PKCS#12 (PFX) содержит и закрытый, и открытый ключ — хранится на файловом сервере
			var pfxBytes = certificate.Export(X509ContentType.Pkcs12);

			// PEM-представление публичного сертификата — хранится в БД для быстрого доступа
			var publicCertPem = certificate.ExportCertificatePem();

			var filePath = await _fileStorage.SaveCertificateAsync(userId, serialNumber, pfxBytes, "pfx");

			return new CertificateBindingModel
			{
				StartDate = startDate.UtcDateTime,
				FinishDate = endDate.UtcDateTime,
				PublicKey = publicCertPem,
				Publisher = publisher,
				Owner = owner,
				Number = serialNumber,
				UserId = userId,
				IsActual = true,
				Mode = CertificateMode.Internal,
				FilePath = filePath,
			};
		}

		/// <summary>
		/// Генерирует криптографически случайный серийный номер сертификата (16 байт → 32 hex-символа).
		/// Старший бит обнуляется, чтобы серийный номер интерпретировался как положительное целое.
		/// </summary>
		private static string GenerateSerialNumber()
		{
			Span<byte> bytes = stackalloc byte[16];
			RandomNumberGenerator.Fill(bytes);
			bytes[0] &= 0x7F;
			return Convert.ToHexString(bytes).ToLowerInvariant();
		}

		/// <summary>
		/// Экранирует спецсимволы в значении RDN согласно RFC 4514.
		/// </summary>
		private static string EscapeRdnValue(string value) =>
			value.Replace("\\", "\\\\")
			     .Replace(",", "\\,")
			     .Replace("+", "\\+")
			     .Replace("\"", "\\\"")
			     .Replace("<", "\\<")
			     .Replace(">", "\\>")
			     .Replace(";", "\\;");
	}
}
