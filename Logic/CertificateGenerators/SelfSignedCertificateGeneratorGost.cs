using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.StorageContracts;
using Models;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.CryptoPro;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System.Security.Cryptography;

namespace Logic.CertificateGenerators
{
	/// <summary>
	/// Генерирует самоподписанные сертификаты для внутреннего режима (ГОСТ Р 34.10-2012 / ГОСТ Р 34.11-2012 через BouncyCastle).
	/// Файл ключевой пары — PKCS#12 (.pfx) на файловом сервере; публичная часть — PEM в БД.
	/// </summary>
	public class SelfSignedCertificateGeneratorGost : ICertificateGeneratorLogic
	{
		/// <summary>Имя кривой TC26 для ключа 256 бит (ГОСТ Р 34.10-2012).</summary>
		private const string Gost2012_256_CurveName = "Tc26-Gost-3410-12-256-paramSetA";

		/// <summary>Связка «Стрибог-256 + подпись ECGOST3410-2012-256» в терминах BouncyCastle.</summary>
		private const string GostSignatureAlgorithm = "GOST3411-2012-256WITHECGOST3410-2012-256";

		/// <summary>Псевдоним записи в PKCS#12 (для экспорта PFX).</summary>
		private const string PfxAlias = "internal-gost";

		/// <summary>Файловое хранилище PFX.</summary>
		private readonly IFileStorage _fileStorage;

		public SelfSignedCertificateGeneratorGost(IFileStorage fileStorage)
		{
			_fileStorage = fileStorage; // сохраняем зависимость для записи .pfx на диск
		}

		/// <inheritdoc />
		public async Task<CertificateBindingModel> GenerateSelfSignedAsync(int userId, string owner, string publisher)
		{
			if (userId <= 0) // идентификатор пользователя должен быть положительным
				throw new ArgumentException("Не указан идентификатор пользователя", nameof(userId));
			if (string.IsNullOrWhiteSpace(owner)) // без владельца сертификат смысла не имеет
				throw new ArgumentException("Не указан владелец сертификата", nameof(owner));
			if (string.IsNullOrWhiteSpace(publisher)) // без издателя DN будет неполным
				throw new ArgumentException("Не указан издатель сертификата", nameof(publisher));

			var random = new SecureRandom(); // криптографический ГПСЧ для ключей и подписи TBSCertificate
			var x9 = ECGost3410NamedCurves.GetByName(Gost2012_256_CurveName) // параметры эллиптической кривой ГОСТ 2012 (256)
				?? throw new InvalidOperationException($"Неизвестная кривая: {Gost2012_256_CurveName}");
			var domain = new ECDomainParameters(x9.Curve, x9.G, x9.N, x9.H, x9.GetSeed()); // домен EC: кривая, базовая точка, порядок, кофактор, seed
			var keyGen = new ECKeyPairGenerator(); // генератор пары ключей на заданной кривой
			keyGen.Init(new ECKeyGenerationParameters(domain, random)); // инициализация доменом и источником случайности
			var keyPair = keyGen.GenerateKeyPair(); // получаем открытый и закрытый ключ ГОСТ 2012

			var serialHex = GenerateSerialNumber(); // строковый серийный номер (hex), как в RSA-версии
			var serialBigInt = new BigInteger(1, Convert.FromHexString(serialHex)); // положительный BigInteger для поля SerialNumber в X.509

			var startDate = DateTimeOffset.UtcNow; // момент начала действия сертификата (UTC)
			var endDate = startDate.AddYears(1); // срок действия — один год, как в стандартном генераторе

			var subjectIssuer = new X509Name($"CN={EscapeRdnValue(owner)}, O={EscapeRdnValue(publisher)}"); // Distinguished Name издателя и субъекта (самоподпись)
			var certGen = new X509V3CertificateGenerator(); // генератор сертификата версии 3 с расширениями
			certGen.SetSerialNumber(serialBigInt); // задаём серийный номер
			certGen.SetIssuerDN(subjectIssuer); // издатель = мы сами (самоподписанный)
			certGen.SetSubjectDN(subjectIssuer); // субъект совпадает с издателем
			certGen.SetNotBefore(startDate.UtcDateTime); // дата «не ранее»
			certGen.SetNotAfter(endDate.UtcDateTime); // дата «не позднее»
			certGen.SetPublicKey(keyPair.Public); // вставляем открытый ключ из сгенерированной пары

			certGen.AddExtension(X509Extensions.BasicConstraints, critical: false, new BasicConstraints(cA: false)); // не УЦ, только конечный субъект
			certGen.AddExtension(X509Extensions.KeyUsage, critical: true, // назначение ключа: только ЭП и неотрекаемость
				new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.NonRepudiation));

			var spki = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public); // ASN.1 SubjectPublicKeyInfo от открытого ключа
			var skiDigest = DigestUtilities.CalculateDigest("SHA-1", spki.PublicKey.GetOctets()); // SKI: SHA-1 от битовой строки ключа (как у Microsoft)
			certGen.AddExtension(X509Extensions.SubjectKeyIdentifier, critical: false, new DerOctetString(skiDigest)); // расширение Subject Key Identifier

			var sigFactory = new Asn1SignatureFactory(GostSignatureAlgorithm, keyPair.Private, random); // фабрика подписи TBSCertificate на ГОСТ
			var bcCertificate = certGen.Generate(sigFactory); // собираем DER-сертификат с подписью

			var pfxBytes = ExportToPkcs12(bcCertificate, keyPair.Private, random); // упаковываем сертификат + закрытый ключ в PKCS#12
			var publicCertPem = ExportCertificatePem(bcCertificate); // PEM только публичной части для БД

			var filePath = await _fileStorage.SaveCertificateAsync(userId, serialHex, pfxBytes, "pfx"); // сохраняем PFX на файловый сервер

			return new CertificateBindingModel // модель для записи в БД (аналог RSA-генератора)
			{
				StartDate = startDate.UtcDateTime, // начало действия
				FinishDate = endDate.UtcDateTime, // окончание действия
				PublicKey = publicCertPem, // PEM сертификата
				Publisher = publisher, // издатель (строка)
				Owner = owner, // владелец (строка)
				Number = serialHex, // серийный номер в hex
				UserId = userId, // пользователь-владелец
				IsActual = true, // активный сертификат
				Mode = CertificateMode.Internal, // внутренний режим системы
				FilePath = filePath, // относительный путь к PFX
			};
		}

		/// <summary>Собирает PKCS#12 без пароля (пустой пароль контейнера).</summary>
		private static byte[] ExportToPkcs12(X509Certificate certificate, AsymmetricKeyParameter privateKey, SecureRandom random)
		{
			var store = new Pkcs12StoreBuilder().Build(); // пустое хранилище PKCS#12
			var certEntry = new X509CertificateEntry(certificate); // запись сертификата для цепочки
			store.SetKeyEntry(PfxAlias, new AsymmetricKeyEntry(privateKey), new[] { certEntry }); // привязываем закрытый ключ и цепочку из одного cert
			using var ms = new MemoryStream(); // поток для сериализации .pfx
			store.Save(ms, Array.Empty<char>(), random); // сохраняем без пароля (пустой массив символов пароля)
			return ms.ToArray(); // байты PFX для файла
		}

		/// <summary>PEM «BEGIN CERTIFICATE» для поля PublicKey в БД.</summary>
		private static string ExportCertificatePem(X509Certificate certificate)
		{
			using var sw = new StringWriter(); // текстовый буфер для PEM
			var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(sw); // писатель PEM в формате OpenSSL
			pemWriter.WriteObject(certificate); // записываем объект сертификата
			pemWriter.Writer.Flush(); // сбрасываем в StringWriter
			return sw.ToString(); // итоговая строка PEM
		}

		/// <summary>Случайный серийный номер (16 байт, старший бит сброшен), hex в нижнем регистре.</summary>
		private static string GenerateSerialNumber()
		{
			Span<byte> bytes = stackalloc byte[16]; // 16 байт энтропии под серийник
			RandomNumberGenerator.Fill(bytes); // криптостойкое заполнение
			bytes[0] &= 0x7F; // старший бит в ноль — серийник всегда положительный при интерпретации как BigInteger
			return Convert.ToHexString(bytes).ToLowerInvariant(); // 32 hex-символа в нижнем регистре
		}

		/// <summary>Экранирование значений RDN по RFC 4514.</summary>
		private static string EscapeRdnValue(string value) =>
			value.Replace("\\", "\\\\") // обратный слэш
			     .Replace(",", "\\,") // запятая в RDN
			     .Replace("+", "\\+") // плюс
			     .Replace("\"", "\\\"") // кавычка
			     .Replace("<", "\\<") // угловая скобка
			     .Replace(">", "\\>") // угловая скобка
			     .Replace(";", "\\;"); // точка с запятой
	}
}
