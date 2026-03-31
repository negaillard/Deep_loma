using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.StorageContracts
{
	public interface IFileStorage
	{
		Task<string> SaveOriginalAsync(int documentId, string title, Stream stream, string extension);
		/// <summary>
		/// Сохраняет detached-подпись в каталоге документа: documents/{title}/signatures/{userId}.sig
		/// </summary>
		Task<string> SaveSignatureAsync(int documentId, string documentTitle, int userId, Stream stream);

		/// <summary>
		/// Сохраняет публичный сертификат (.cer), извлечённый из подписи.
		/// documents/{title}/certificates/{userId}.cer
		/// </summary>
		Task<string> SaveSignatureCertificateAsync(int documentId, string documentTitle, int userId, byte[] cerBytes);

		Task<Stream> GetFileAsync(string relativePath);
		/// <summary>Удаляет каталог документа documents/{title}/ (оригинал, подписи, сертификаты из подписей).</summary>
		Task DeleteDocumentFolderAsync(string documentTitle);

		/// <summary>
		/// Сохраняет файл сертификата. extension: "pfx" для внутреннего, "cer" для внешнего.
		/// Возвращает относительный путь к файлу.
		/// </summary>
		Task<string> SaveCertificateAsync(int userId, string certificateNumber, byte[] data, string extension);

		/// <summary>
		/// Читает байты файла сертификата по относительному пути.
		/// </summary>
		Task<byte[]> GetCertificateBytesAsync(string relativePath);

		/// <summary>
		/// Удаляет файл сертификата по относительному пути.
		/// </summary>
		Task DeleteCertificateAsync(string relativePath);
	}
}
