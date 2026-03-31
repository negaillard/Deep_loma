using Contracts.StorageContracts;
using Microsoft.Extensions.Configuration;

namespace FileStorage
{
	public class LocalFileStorage : IFileStorage
	{
		private readonly string _rootPath;

		public LocalFileStorage(IConfiguration configuration)
		{
			_rootPath = configuration["FileStorage:RootPath"]
				?? throw new InvalidOperationException("FileStorage:RootPath не задан в конфигурации");
		}

		public async Task<string> SaveOriginalAsync(int documentId, string title, Stream stream, string extension)
		{
			var safeTitle = SanitizeFolderName(title);
			var folder = Path.Combine(_rootPath, "documents", safeTitle);
			Directory.CreateDirectory(folder);

			var filePath = Path.Combine(folder, $"original{extension}");

			using var fileStream = File.Create(filePath);
			await stream.CopyToAsync(fileStream);

			return GetRelativePath(filePath);
		}
		public async Task<string> SaveSignatureAsync(int documentId, string documentTitle, int userId, Stream stream)
		{
			var safeTitle = SanitizeFolderName(documentTitle);
			var folder = Path.Combine(_rootPath, "documents", safeTitle, "signatures");
			Directory.CreateDirectory(folder);

			var filePath = Path.Combine(folder, $"{userId}.sig");

			using var fileStream = File.Create(filePath);
			await stream.CopyToAsync(fileStream);

			return GetRelativePath(filePath);
		}

		public async Task<string> SaveSignatureCertificateAsync(int documentId, string documentTitle, int userId, byte[] cerBytes)
		{
			var safeTitle = SanitizeFolderName(documentTitle);
			var folder = Path.Combine(_rootPath, "documents", safeTitle, "certificates");
			Directory.CreateDirectory(folder);

			var filePath = Path.Combine(folder, $"{userId}.cer");
			await File.WriteAllBytesAsync(filePath, cerBytes);

			return GetRelativePath(filePath);
		}

		public Task<Stream> GetFileAsync(string relativePath)
		{
			var fullPath = Path.Combine(_rootPath, relativePath);
			Stream stream = File.OpenRead(fullPath);
			return Task.FromResult(stream);
		}

		public Task DeleteDocumentFolderAsync(string documentTitle)
		{
			var safeTitle = SanitizeFolderName(documentTitle);
			var folder = Path.Combine(_rootPath, "documents", safeTitle);

			if (Directory.Exists(folder))
				Directory.Delete(folder, true);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Сохраняет файл сертификата.
		/// Внутренний режим: extension = "pfx" → certificates/{userId}/{number}.pfx
		/// </summary>
		public async Task<string> SaveCertificateAsync(int userId, string certificateNumber, byte[] data, string extension)
		{
			var folder = Path.Combine(_rootPath, "certificates", userId.ToString());
			Directory.CreateDirectory(folder);

			var filePath = Path.Combine(folder, $"{certificateNumber}.{extension}");

			await File.WriteAllBytesAsync(filePath, data);

			return GetRelativePath(filePath);
		}

		public async Task<byte[]> GetCertificateBytesAsync(string relativePath)
		{
			var fullPath = Path.Combine(_rootPath, relativePath);
			return await File.ReadAllBytesAsync(fullPath);
		}

		public Task DeleteCertificateAsync(string relativePath)
		{
			var fullPath = Path.Combine(_rootPath, relativePath);

			if (File.Exists(fullPath))
				File.Delete(fullPath);

			return Task.CompletedTask;
		}

		private string GetRelativePath(string fullPath)
		{
			return Path.GetRelativePath(_rootPath, fullPath);
		}

		public static string SanitizeFolderName(string title)
		{
			if (string.IsNullOrWhiteSpace(title))
				return "document";

			var invalidChars = Path.GetInvalidFileNameChars();

			var cleaned = new string(title
				.Where(c => !invalidChars.Contains(c))
				.ToArray());

			cleaned = cleaned.Trim();

			if (cleaned.Length > 100)
				cleaned = cleaned.Substring(0, 100);

			if (string.IsNullOrWhiteSpace(cleaned))
				cleaned = "document";

			return cleaned;
		}
	}
}
