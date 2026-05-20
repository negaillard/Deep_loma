using Contracts.ViewModels;
using Contracts.StorageContracts;
using Contracts.LogicContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Contracts.SearchModels;

namespace API.Helpers
{
	public static class VerificationPackageHelper
	{
		public static async Task<byte[]> GenerateVerificationPackageZipAsync(
			DocumentViewModel document,
			List<SignatureViewModel> signatures,
			IFileStorage fileStorage,
			IUserLogic userLogic,
			ICertificateLogic certificateLogic,
			ILogger logger)
		{
			var readmeSigners = new List<(string SignerName, DateTime SignedAt)>();

			using var memoryStream = new MemoryStream();
			using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
			{
				// 1. Оригинальный документ
				await using (var docStream = await fileStorage.GetFileAsync(document.Path))
				{
					var ext = Path.GetExtension(document.Path);
					var entry = archive.CreateEntry($"document{ext}");
					await using var entryStream = entry.Open();
					await docStream.CopyToAsync(entryStream);
				}

				// 2. Версия для печати со штампами подписей (только для PDF)
				var extension = Path.GetExtension(document.Path);
				if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
				{
					var stampInfos = new List<PdfStampInfo>();
					foreach (var sig in signatures.Where(s => !s.IsDeleted))
					{
						var user = await userLogic.ReadElementAsync(new UserSearchModel { Id = sig.UserId });
						var cert = await certificateLogic.ReadElementAsync(new CertificateSearchModel { Id = sig.CerificateId });

						var stampInfo = await GetStampInfoFromSignatureAsync(sig, user, cert, fileStorage, logger);
						stampInfos.Add(stampInfo);
					}

					try
					{
						logger.LogInformation("Генерация печатной версии со штампами для документа {Id}", document.Id);
						using (var originalDocStream = await fileStorage.GetFileAsync(document.Path))
						using (var stampedPdfStream = PdfStampsHelper.StampPdf(originalDocStream, stampInfos))
						{
							var printEntry = archive.CreateEntry("document_print.pdf");
							await using var printEntryStream = printEntry.Open();
							await stampedPdfStream.CopyToAsync(printEntryStream);
						}
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Ошибка при генерации печатной версии документа {Id}", document.Id);
					}
				}

				// 3. Подписи 
				foreach (var sig in signatures.Where(s => !s.IsDeleted))
				{
					var user = await userLogic.ReadElementAsync(new UserSearchModel { Id = sig.UserId });
					var safeName = SanitizeName(user?.Fullname ?? sig.UserId.ToString());
					var signerForReadme = string.IsNullOrWhiteSpace(user?.Fullname)
						? $"UserId={sig.UserId}"
						: user.Fullname.Trim();
					readmeSigners.Add((signerForReadme, sig.SignedAt));

					// файл подписи
					if (!string.IsNullOrEmpty(sig.Path))
					{
						try
						{
							await using var sigStream = await fileStorage.GetFileAsync(sig.Path);

							using var sigBuffer = new MemoryStream();
							await sigStream.CopyToAsync(sigBuffer);
							sigBuffer.Position = 0;

							// Сохраняем файл подписи
							var sigEntry = archive.CreateEntry($"signatures/{safeName}.sig");
							await using var sigEntryStream = sigEntry.Open();
							sigBuffer.Position = 0;
							await sigBuffer.CopyToAsync(sigEntryStream);
						}
						catch (Exception ex)
						{
							logger.LogWarning(ex, "Не удалось обработать файл подписи {Path}", sig.Path);
						}
					}
				}

				// 4. Инструкция по верификации (README.txt)
				var readmeEntry = archive.CreateEntry("README.txt");
				await using var readmeStream = readmeEntry.Open();
				await using var writer = new StreamWriter(readmeStream);
				await writer.WriteAsync(BuildReadme(document.Title, readmeSigners));
			}

			return memoryStream.ToArray();
		}

		private static async Task<PdfStampInfo> GetStampInfoFromSignatureAsync(
			SignatureViewModel sig,
			UserViewModel? user,
			CertificateViewModel? cert,
			IFileStorage fileStorage,
			ILogger logger)
		{
			var info = new PdfStampInfo
			{
				CertNumber = cert?.Number ?? "Не указан",
				Owner = cert?.Owner ?? user?.Fullname ?? "Не указан",
				ValidFrom = cert?.StartDate ?? sig.SignedAt.AddYears(-1),
				ValidTo = cert?.FinishDate ?? sig.SignedAt.AddYears(1)
			};

			if ((info.CertNumber == "Не указан" || info.Owner == "Не указан" || cert == null) && !string.IsNullOrEmpty(sig.Path))
			{
				try
				{
					byte[] sigBytes;
					await using (var sigStream = await fileStorage.GetFileAsync(sig.Path))
					{
						using (var ms = new MemoryStream())
						{
							await sigStream.CopyToAsync(ms);
							sigBytes = ms.ToArray();
						}
					}

					byte[]? certDer = null;
					try
					{
						var signedCms = new SignedCms();
						signedCms.Decode(sigBytes);
						if (signedCms.Certificates.Count > 0)
						{
							certDer = signedCms.Certificates[0].RawData;
						}
						else if (signedCms.SignerInfos.Count > 0)
						{
							var c = signedCms.SignerInfos[0].Certificate;
							if (c != null)
								certDer = c.RawData;
						}
					}
					catch
					{
						// Игнорируем ошибки стандартного SignedCms
					}

					if (certDer == null && Logic.Pkcs7SignerCertificateExtractor.TryGetSignerCertificateDer(sigBytes, out var bcDer))
					{
						certDer = bcDer;
					}

					if (certDer != null)
					{
						using (var x509 = new System.Security.Cryptography.X509Certificates.X509Certificate2(certDer))
						{
							info.CertNumber = x509.SerialNumber;
							info.Owner = ParseCN(x509.Subject);
							info.ValidFrom = x509.NotBefore;
							info.ValidTo = x509.NotAfter;
						}
					}
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Ошибка извлечения сертификата из файла подписи для подписи {SigId}", sig.Id);
				}
			}

			if ((string.IsNullOrEmpty(info.Owner) || info.Owner == "Не указан") && user != null)
			{
				info.Owner = user.Fullname;
			}

			return info;
		}

		private static string ParseCN(string subjectName)
		{
			if (string.IsNullOrEmpty(subjectName)) return "Не указан";
			var match = System.Text.RegularExpressions.Regex.Match(subjectName, @"CN=([^,]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			return match.Success ? match.Groups[1].Value.Trim() : subjectName;
		}

		private static string SanitizeName(string name)
		{
			var invalid = Path.GetInvalidFileNameChars();
			return new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
		}

		private static string BuildReadme(string title, IReadOnlyList<(string SignerName, DateTime SignedAt)> signers)
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"Пакет верификации документа: {title}");
			sb.AppendLine($"Дата формирования: {DateTime.UtcNow:dd.MM.yyyy HH:mm} UTC");
			sb.AppendLine();
			sb.AppendLine("Состав пакета:");
			sb.AppendLine("  document.*          — оригинальный документ");
			sb.AppendLine("  signatures/*.sig    — отсоединённые подписи (PKCS#7 DER)");
			sb.AppendLine();
			sb.AppendLine("Проверка подписи (КриптоПро CSP):");
			sb.AppendLine("  csptest -sfsign -verify -in document.* -signature signatures/<ФИО>.sig -detached");
			sb.AppendLine();
			sb.AppendLine("Проверка подписи (OpenSSL, только для RSA):");
			sb.AppendLine("  openssl smime -verify -inform DER -in signatures/<ФИО>.sig -content document.* -noverify");
			sb.AppendLine();
			sb.AppendLine("Подписанты (ФИО в системе):");
			foreach (var (signerName, signedAt) in signers)
			{
				sb.AppendLine($"  - {signerName}, подписано {signedAt:dd.MM.yyyy HH:mm} UTC");
			}
			return sb.ToString();
		}
	}
}
