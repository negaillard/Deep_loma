using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using MassTransit;
using MessageContracts;
using Microsoft.Extensions.Logging;
using Models;

namespace SigningService.Consumers
{
	/// <summary>
	/// Обрабатывает запросы на криптографическое подписание документов.
	/// Получает SigningRequestMessage из RabbitMQ, делегирует подписание IDocumentSigner
	/// и сохраняет результат в БД и файловое хранилище.
	/// </summary>
	public class SignDocumentConsumer : IConsumer<SigningRequestMessage>
	{
		private readonly IDocumentStorage _documentStorage;
		private readonly ICertificateStorage _certificateStorage;
		private readonly IDocumentUserStorage _documentUserStorage;
		private readonly ISignatureStorage _signatureStorage;
		private readonly IFileStorage _fileStorage;
		private readonly IDocumentSigner _documentSigner;
		private readonly IPublishEndpoint _publishEndpoint;
		private readonly ILogger<SignDocumentConsumer> _logger;

		public SignDocumentConsumer(
			IDocumentStorage documentStorage,
			ICertificateStorage certificateStorage,
			IDocumentUserStorage documentUserStorage,
			ISignatureStorage signatureStorage,
			IFileStorage fileStorage,
			IDocumentSigner documentSigner,
			IPublishEndpoint publishEndpoint,
			ILogger<SignDocumentConsumer> logger)
		{
			_documentStorage = documentStorage;
			_certificateStorage = certificateStorage;
			_documentUserStorage = documentUserStorage;
			_signatureStorage = signatureStorage;
			_fileStorage = fileStorage;
			_documentSigner = documentSigner;
			_publishEndpoint = publishEndpoint;
			_logger = logger;
		}

		public async Task Consume(ConsumeContext<SigningRequestMessage> context)
		{
			var message = context.Message;
			_logger.LogInformation(
				"Получен запрос на подписание: DocumentId={DocumentId}, UserId={UserId}",
				message.DocumentId, message.UserId);

			try
			{
				var document = await _documentStorage.GetElementAsync(
					new DocumentSearchModel { Id = message.DocumentId });

				if (document == null || document.IsDeleted)
				{
					_logger.LogWarning("Документ {DocumentId} не найден или удалён", message.DocumentId);
					await SetFailedAsync(message);
					return;
				}

				var certificate = await _certificateStorage.GetElementAsync(
					new CertificateSearchModel { UserId = message.UserId, IsActual = true });

				if (certificate == null)
				{
					_logger.LogWarning(
						"Активный сертификат для пользователя {UserId} не найден", message.UserId);
					await SetFailedAsync(message);
					return;
				}

				if (certificate.FinishDate < DateTime.UtcNow)
				{
					_logger.LogWarning(
						"Сертификат пользователя {UserId} истёк {FinishDate}",
						message.UserId, certificate.FinishDate);
					await SetFailedAsync(message);
					return;
				}

				var documentBytes = await ReadAllBytesAsync(document.Path);

				// делегируем подписание — алгоритм скрыт за интерфейсом
				var signatureBytes = await _documentSigner.SignAsync(documentBytes, certificate);
				var signatureHex = Convert.ToHexString(signatureBytes).ToLowerInvariant();

				using var sigStream = new MemoryStream(signatureBytes);

				var signatureModel = new SignatureBindingModel
				{
					SignatureValue = signatureHex,
					CerificateId = certificate.Id,
					SignedAt = DateTime.UtcNow,
					UserId = message.UserId,
					DocumentId = message.DocumentId,
					IsDeleted = false,
					//CertificatePath = 
				};

				var created = await _signatureStorage.InsertAsync(signatureModel);
				if (created == null)
				{
					_logger.LogError(
						"Не удалось сохранить запись подписи для DocumentId={DocumentId}, UserId={UserId}",
						message.DocumentId, message.UserId);
					await SetFailedAsync(message);
					return;
				}

			signatureModel.Id = created.Id;
			signatureModel.Path = await _fileStorage.SaveSignatureAsync(
				message.DocumentId, document.Title, message.UserId, sigStream);

			signatureModel.CertificatePath = await ExtractAndSaveCertificateAsync(
				signatureBytes, message.DocumentId, document.Title, message.UserId);

			await _signatureStorage.UpdateAsync(signatureModel);

				var documentUser = await _documentUserStorage.GetElementAsync(
					new DocumentUserSearchModel
					{
						UserId = message.UserId,
						DocumentId = message.DocumentId
					});

			if (documentUser != null)
			{
				await _documentUserStorage.UpdateAsync(new DocumentUserBindingModel
				{
					Id = documentUser.Id,
					UserId = documentUser.UserId,
					DocumentId = documentUser.DocumentId,
					AssignedAt = documentUser.AssignedAt,
					SigningStatus = SigningStatus.SIGNED,
					Order = documentUser.Order
				});

				// при последовательном режиме уведомляем следующего подписанта
				if (documentUser.Order > 0)
				{
					var allSigners = await _documentUserStorage.GetFilteredListAsync(
						new DocumentUserSearchModel { DocumentId = message.DocumentId });

					var nextSigner = allSigners?.FirstOrDefault(du => du.Order == documentUser.Order + 1);
					if (nextSigner != null)
					{
						await _publishEndpoint.Publish(new NotificationMessage(
							UserId: nextSigner.UserId,
							Title: document.Title,
							RequestedAt: DateTime.UtcNow));

						_logger.LogInformation(
							"Уведомление отправлено следующему подписанту UserId={UserId} (Order={Order})",
							nextSigner.UserId, nextSigner.Order);
					}
				}
			}

			_logger.LogInformation(
				"Документ {DocumentId} успешно подписан пользователем {UserId}, SignatureId={SignatureId}",
				message.DocumentId, message.UserId, created.Id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex,
					"Ошибка при подписании документа {DocumentId} пользователем {UserId}",
					message.DocumentId, message.UserId);

				await SetFailedAsync(message);
			}
		}

		/// <summary>
	/// Извлекает публичный сертификат подписанта из PKCS#7 и сохраняет как .cer.
	/// Работает для RSA (Internal) и ГОСТ (External) подписей.
	/// </summary>
	private async Task<string> ExtractAndSaveCertificateAsync(
		byte[] signatureBytes, int documentId, string documentTitle, int userId)
	{
		try
		{
			var signedCms = new System.Security.Cryptography.Pkcs.SignedCms();
			signedCms.Decode(signatureBytes);

			if (signedCms.Certificates.Count == 0)
			{
				_logger.LogWarning(
					"Подпись UserId={UserId} не содержит сертификата — .cer не сохранён", userId);
				return string.Empty;
			}

			var cerBytes = signedCms.Certificates[0].RawData;
			return await _fileStorage.SaveSignatureCertificateAsync(documentId, documentTitle, userId, cerBytes);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex,
				"Не удалось извлечь сертификат из подписи UserId={UserId}", userId);
			return string.Empty;
		}
	}

	private async Task<byte[]> ReadAllBytesAsync(string relativePath)
		{
			using var stream = await _fileStorage.GetFileAsync(relativePath);
			using var ms = new MemoryStream();
			await stream.CopyToAsync(ms);
			return ms.ToArray();
		}

		private async Task SetFailedAsync(SigningRequestMessage message)
		{
			var documentUser = await _documentUserStorage.GetElementAsync(
				new DocumentUserSearchModel
				{
					UserId = message.UserId,
					DocumentId = message.DocumentId
				});

			if (documentUser == null || documentUser.SigningStatus != SigningStatus.PENDING)
				return;

		await _documentUserStorage.UpdateAsync(new DocumentUserBindingModel
		{
			Id = documentUser.Id,
			UserId = documentUser.UserId,
			DocumentId = documentUser.DocumentId,
			AssignedAt = documentUser.AssignedAt,
			SigningStatus = SigningStatus.NOT_SIGNED,
			Order = documentUser.Order
		});

			_logger.LogWarning(
				"Статус подписи для DocumentId={DocumentId}, UserId={UserId} сброшен в NOT_SIGNED после ошибки",
				message.DocumentId, message.UserId);
		}
	}
}
