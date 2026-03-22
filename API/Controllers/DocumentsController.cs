
using API.Authorization;
using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Contracts.StorageContracts;
using Contracts.ViewModels;
using MassTransit;
using MessageContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Models;
using System;
using System.IO;

namespace API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class DocumentsController : ControllerBase
	{
		private readonly IDocumentLogic _documentLogic;
		private readonly IDocumentUserLogic _documentUserLogic;
		private readonly IFileStorage _fileStorage;
		private readonly ILogger<DocumentsController> _logger;
		private readonly IAntivirusService _antivirus;
		private readonly FileUploadPolicy _filePolicy;
		private readonly IPublishEndpoint _publishEndpoint;

		public DocumentsController(
			IDocumentLogic documentLogic,
			IDocumentUserLogic documentUserLogic,
			IFileStorage fileStorage,
			IAntivirusService antivirus,
			IOptions<FileUploadPolicy> filePolicy,
			ILogger<DocumentsController> logger,
			IPublishEndpoint publishEndpoint)
		{
			_documentLogic = documentLogic;
			_documentUserLogic = documentUserLogic;
			_fileStorage = fileStorage;
			_antivirus = antivirus;
			_filePolicy = filePolicy.Value;
			_logger = logger;
			_publishEndpoint = publishEndpoint;
		}

		[AuthorizeSigner]
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			_logger.LogInformation("Попытка получения списка документов");
			return Ok(await _documentLogic.ReadListAsync(null));
		}

		[AuthorizeSigner]
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			try
			{
				_logger.LogInformation($"Попытка получения документа по id{id}");
				var document = await _documentLogic.ReadElementAsync(new DocumentSearchModel { Id = id });
				if (document == null)
				{
					_logger.LogWarning($"документ по id{id} не найден");
					return NotFound();
				}
				_logger.LogInformation($"документ по id{id} найден");
				return Ok(document);
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка получения документа " + ex.Message);
			}
		}

	[AuthorizeDocument]
	[HttpPost]
	[RequestSizeLimit(100_000_000)]
	public async Task<IActionResult> Create(
		[FromForm] string title,
		[FromForm] string description,
		[FromForm] List<int> userIds,
		[FromForm] bool isSequential = false,
		IFormFile? file = null)
		{
			try
			{
				if (file == null || file.Length == 0)
				{
					return BadRequest("Не передан файл документа");
				}

				var user = HttpContext.Items["User"] as UserViewModel;
				if (user == null)
				{
					return Unauthorized("Требуется авторизация");
				}
				/// проверка расширений
				string extension = Path.GetExtension(file.FileName).ToLower();

				if (!_filePolicy.AllowedExtensions.Contains(extension))
				{
					_logger.LogWarning("Попытка загрузки запрещенного типа файла: {Ext}", extension);
					return BadRequest("Тип файла запрещён политикой безопасности");
				}

				/// проверка на вирусы
				using var stream = file.OpenReadStream();

				_logger.LogInformation("Проверка файла на вирусы");

				if (!await _antivirus.IsFileCleanAsync(stream))
				{
					_logger.LogWarning("Вирус обнаружен в файле {FileName}", file.FileName);
					return BadRequest("Файл содержит вирус");
				}

				stream.Position = 0;
				/// проверка закончена


				var model = new DocumentBindingModel
				{
					Title = title,
					Description = description,
					UserIds = userIds,
					CreatedByUserId = user?.Id ?? 0,
					CreatedAt = DateTime.UtcNow,
					Status = DocumentStatus.NOT_SIGNED,
					IsDeleted = false,
					IsSequential = isSequential,
				};

				_logger.LogInformation("Попытка создания документа '{Title}'", model.Title);
				//string extension = Path.GetExtension(file.FileName);
				//using var stream = file.OpenReadStream();
				if (!await _documentLogic.CreateAsync(model, stream, extension))
				{
					_logger.LogWarning("Документ '{Title}' не был создан", model.Title);
					return BadRequest("Ошибка при создании документа");
				}
				_logger.LogInformation("Документ '{Title}' успешно создан", model.Title);

				if (isSequential)
				{
					// при последовательном режиме уведомляем только первого подписанта
					await _publishEndpoint.Publish(new NotificationMessage(
						UserId: model.UserIds[0],
						Title: title,
						RequestedAt: DateTime.UtcNow));
				}
				else
				{
					// иначе уведомляем всех
					foreach (int userId in model.UserIds)
					{
						await _publishEndpoint.Publish(new NotificationMessage(
							UserId: userId,
							Title: title,
							RequestedAt: DateTime.UtcNow));
					}
				}
				

				return Ok("Документ создан");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при создании документа " + ex.Message);
			}
		}

		[AuthorizeDocument]
		[HttpPut]
		public async Task<IActionResult> Update([FromBody] DocumentBindingModel model)
		{
			try
			{
				if (!await _documentLogic.UpdateAsync(model))
				{
					_logger.LogWarning($"Документ c id{model.Id} не был обновлен");
					return BadRequest("Ошибка при обновлении документа");
				}
				_logger.LogInformation($"Документ c id{model.Id} был обновлен");
				return Ok("Документ обновлён");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при обновлении документа " + ex.Message);
			}
		}

		[AuthorizeDocument]
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				if (!await _documentLogic.DeleteAsync(new DocumentBindingModel { Id = id }))
				{
					_logger.LogWarning($"Документ c id{id} не был удален");
					return BadRequest("Ошибка при удалении документа");
				}
				_logger.LogInformation($"документ c id{id} был удален");
				return Ok("Документ удалён");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при удалении документа " + ex.Message);
			}
		}

		[AuthorizeSigner]
		[HttpGet("filter")]
		public async Task<IActionResult> Filter(
			[FromQuery] string? title = null,
			[FromQuery] int? createdByUserId = null,
			[FromQuery] DocumentStatus? status = null,
			[FromQuery] bool? isDeleted = null)
		{
			_logger.LogInformation("Фильтрация документов");
			var result = await _documentLogic.ReadListAsync(new DocumentSearchModel
			{
				Title = title,
				CreatedByUserId = createdByUserId,
				Status = status,
				IsDeleted = isDeleted
			});
			return Ok(result);
		}

		[AuthorizeSigner]
		[HttpGet("paged")]
		public async Task<IActionResult> GetPaged(
			[FromQuery] int pageNumber = 1,
			[FromQuery] int pageSize = 20)
		{
			_logger.LogInformation("Получение документов с пагинацией");
			var result = await _documentLogic.ReadPagedListAsync(new DocumentSearchModel
			{
				PageNumber = pageNumber,
				PageSize = pageSize
			});

			return Ok(result);
		}

		[AuthorizeSigner]
		[HttpGet("{id}/file")]
		public async Task<IActionResult> GetFile(int id)
		{
			try
			{
				_logger.LogInformation($"Попытка скачать файл документа id{id}");
				var document = await _documentLogic.ReadElementAsync(new DocumentSearchModel { Id = id });
				if (document == null || string.IsNullOrWhiteSpace(document.Path))
				{
					return NotFound("Файл документа не найден");
				}

				var stream = await _fileStorage.GetFileAsync(document.Path);
				var fileName = Path.GetFileName(document.Path);
				return File(stream, "application/octet-stream", fileName);
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при скачивании документа " + ex.Message);
			}
		}

	[AuthorizeSigner]
	[HttpGet("get-for-sign")]
	public async Task<IActionResult> GetDocumentsForSign(
		[FromQuery] SigningStatus? signingStatus = null)
	{
		try
		{
			var user = HttpContext.Items["User"] as UserViewModel;
			if (user == null)
				return Unauthorized();

			_logger.LogInformation(
				"Получение документов для подписания пользователем {UserId}", user.Id);

			var documentUsers = await _documentUserLogic.ReadListAsync(
				new DocumentUserSearchModel
				{
					UserId = user.Id,
					SigningStatus = signingStatus
				});

			if (documentUsers == null || documentUsers.Count == 0)
				return Ok(new List<object>());

			var result = new List<object>();

			foreach (var du in documentUsers)
			{
				var document = await _documentLogic.ReadElementAsync(
					new DocumentSearchModel { Id = du.DocumentId });

				if (document == null || document.IsDeleted)
					continue;

				if (document.IsSequential && du.Order > 1)
				{
					var allSigners = await _documentUserLogic.ReadListAsync(
						new DocumentUserSearchModel { DocumentId = du.DocumentId });

					var hasUnfinishedPrevious = allSigners?.Any(s =>
						s.Order < du.Order && s.SigningStatus != SigningStatus.SIGNED) ?? false;

					if (hasUnfinishedPrevious)
						continue;
				}

				result.Add(new
				{
					document.Id,
					document.Title,
					document.Description,
					document.CreatedAt,
					document.Status,
					document.IsSequential,
					UserSigningStatus = du.SigningStatus,
					du.AssignedAt,
					du.Order
				});
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при получении документов для подписания");
			return BadRequest("Ошибка при получении документов: " + ex.Message);
		}
	}
	}

	public class FileUploadPolicy
	{
		public long MaxFileSize { get; set; }
		public List<string> AllowedExtensions { get; set; } = new();
	}
}
