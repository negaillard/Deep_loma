using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.StorageContracts
{
	public interface IFileStorage
	{
		Task<string> SaveOriginalAsync(int documentId, Stream stream);
		Task<string> SaveSignatureAsync(int documentId, int signatureId, Stream stream);
		Task<Stream> GetFileAsync(string relativePath);
		Task DeleteDocumentFolderAsync(int documentId);

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
