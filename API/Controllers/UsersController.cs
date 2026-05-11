using API.Authorization;
using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UsersController : ControllerBase
	{
		private readonly IUserLogic _userLogic;
		private readonly ILogger<UsersController> _logger;

		public UsersController(IUserLogic userLogic, ILogger<UsersController> logger)
		{
			_userLogic = userLogic;
			_logger = logger;
		}

		//[AuthorizeDocument]
		//[HttpGet]
		//public async Task<IActionResult> GetAll()
		//{
		//	_logger.LogInformation("Попытка получения списка пользователей");
		//	return Ok(await _userLogic.ReadListAsync(null));
		//}

		[AuthorizeSigner]
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			try
			{
				_logger.LogInformation($"Попытка получения пользователя по id{id}");
				var user = await _userLogic.ReadElementAsync(new UserSearchModel { Id = id });
				if (user == null)
				{
					_logger.LogWarning($"пользователь по id{id} не найден");
					return NotFound();
				}
				_logger.LogInformation($"пользователь по id{id} найден");
				return Ok(user);
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка получения пользователя: " + ex.Message);
			}
		}

		[AuthorizeAdmin]
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] UserBindingModel model)
		{
			try
			{
				_logger.LogInformation($"Попытка создания пользователя c id{model.Id}");
				if (!await _userLogic.CreateAsync(model))
				{
					_logger.LogWarning($"пользователь c id{model.Id} не был создан");
					return BadRequest("Ошибка при создании пользователя");
				}
				_logger.LogInformation($"пользователь c id{model.Id} был создан");
				return Ok("пользователь создан");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при создании пользователя: " + ex.Message);
			}
		}

		[AuthorizeAdmin]
		[HttpPut]
		public async Task<IActionResult> Update([FromBody] UserBindingModel model)
		{
			try
			{
				if (!await _userLogic.UpdateAsync(model))
				{
					_logger.LogWarning($"пользователь c id{model.Id} не был обновлен");
					return BadRequest("Ошибка при обновлении пользователя");
				}
				_logger.LogInformation($"пользователь c id{model.Id} был обновлен");
				return Ok("пользователь обновлён");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при обновлении пользователя: " + ex.Message);
			}
		}

		[AuthorizeAdmin]
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				if (!await _userLogic.DeleteAsync(new UserBindingModel { Id = id }))
				{
					_logger.LogWarning($"Пользователь c id{id} не был деактивирован");
					return BadRequest("Ошибка при деактивации пользователя");
				}
				_logger.LogInformation($"Пользователь c id{id} был удален");
				return Ok("Пользователь деактивирован");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при деактвации пользователя: " + ex.Message);
			}
		}
		[AuthorizeSigner]
		[HttpGet("filter")]
		public async Task<IActionResult> FilterByFullname([FromQuery] string fullname)
		{
			_logger.LogInformation("Фильтрация пользователей по ФИО");
			if (string.IsNullOrWhiteSpace(fullname))
			{
				return BadRequest("Не указано ФИО для фильтрации");
			}

			var result = await _userLogic.ReadListByFullnameContainsAsync(new UserSearchModel
			{
				Fullname = fullname
			});

			return Ok(result);
		}

		[AuthorizeSigner]
		[HttpGet("paged")]
		public async Task<IActionResult> GetPaged(
			[FromQuery] int pageNumber = 1,
			[FromQuery] int pageSize = 20)
		{
			_logger.LogInformation("Получение пользователей с пагинацией");
			var result = await _userLogic.ReadPagedListAsync(new UserSearchModel
			{
				PageNumber = pageNumber,
				PageSize = pageSize
			});

			return Ok(result);
		}
	}
}
