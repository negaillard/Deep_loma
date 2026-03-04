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

		public async Task<string> SaveOriginalAsync(int documentId, Stream stream)
		{
			var folder = Path.Combine(_rootPath, "documents", documentId.ToString());
			Directory.CreateDirectory(folder);

			var filePath = Path.Combine(folder, "original.pdf");

			using var fileStream = File.Create(filePath);
			await stream.CopyToAsync(fileStream);

			return GetRelativePath(filePath);
		}

		public async Task<string> SaveSignatureAsync(int documentId, int signatureId, Stream stream)
		{
			var folder = Path.Combine(_rootPath, "documents", documentId.ToString(), "signatures");
			Directory.CreateDirectory(folder);

			var filePath = Path.Combine(folder, $"{signatureId}.sig");

			using var fileStream = File.Create(filePath);
			await stream.CopyToAsync(fileStream);

			return GetRelativePath(filePath);
		}

		public Task<Stream> GetFileAsync(string relativePath)
		{
			var fullPath = Path.Combine(_rootPath, relativePath);
			Stream stream = File.OpenRead(fullPath);
			return Task.FromResult(stream);
		}

		public Task DeleteDocumentFolderAsync(int documentId)
		{
			var folder = Path.Combine(_rootPath, "documents", documentId.ToString());

			if (Directory.Exists(folder))
				Directory.Delete(folder, true);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Сохраняет файл сертификата.
		/// Внутренний режим: extension = "pfx" → certificates/{userId}/{number}.pfx
		/// Внешний режим:    extension = "cer" → certificates/{userId}/{number}.cer
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
	}
}
