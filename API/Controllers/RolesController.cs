using API.Authorization;
using Contracts.BindingModels;
using Contracts.LogicContracts;
using Contracts.SearchModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class RolesController : ControllerBase
	{
		private readonly IRoleLogic _roleLogic;
		private readonly ILogger<RolesController> _logger;
		public RolesController(IRoleLogic roleLogic, ILogger<RolesController> logger)
		{
			_roleLogic = roleLogic;
			_logger = logger;
		}

		[AuthorizeDocument]
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			_logger.LogInformation("Попытка получения списка ролей");
			return Ok(await _roleLogic.ReadListAsync(null));
		}

		[AuthorizeDocument]
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			try
			{
				_logger.LogInformation($"Попытка получения роли по id{id}");
				var gph = await _roleLogic.ReadElementAsync(new RoleSearchModel { Id = id });
				if (gph == null)
				{
					_logger.LogWarning($"роль по id{id} не найден");
					return NotFound();
				}
				_logger.LogInformation($"роль по id{id} найден");
				return Ok(gph);
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка получения роли" + ex.Message);
			}

		}

		[AuthorizeAdmin]
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] RoleBindingModel model)
		{
			try
			{
				_logger.LogInformation($"Попытка создания роли c id{model.Id}");
				if (!await _roleLogic.CreateAsync(model))
				{
					_logger.LogWarning($"роль c id{model.Id} не была создана");
					return BadRequest("Ошибка при создании роли");
				}
				_logger.LogInformation($"роль c id{model.Id} была создана");
				return Ok("роль создана");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при создании роли" + ex.Message);
			}
		}

		[AuthorizeAdmin]
		[HttpPut]
		public async Task<IActionResult> Update([FromBody] RoleBindingModel model)
		{
			try
			{
				if (!await _roleLogic.UpdateAsync(model))
				{
					_logger.LogWarning($"роль c id{model.Id} не была обновлена");
					return BadRequest("Ошибка при обновлении рольа");
				}
				_logger.LogInformation($"роль c id{model.Id} была обновлена");
				return Ok("роль обновлён");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при обновлении  роли" + ex.Message);
			}
		}

		[AuthorizeAdmin]
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{

				if (!await _roleLogic.DeleteAsync(new RoleBindingModel { Id = id }))
				{
					_logger.LogWarning($"роль c id{id} не была удалена");
					return BadRequest("Ошибка при удалении роли");
				}
				_logger.LogInformation($"роль c id{id} была удален");
				return Ok("роль удалена");
			}
			catch (Exception ex)
			{
				return BadRequest("Ошибка при удалении роли" + ex.Message);
			}
		}

		[AuthorizeDocument]
		[HttpGet("filter")]
		public async Task<IActionResult> FilterByName([FromQuery] string name)
		{
			_logger.LogInformation("Фильтрация ролей по имени");
			if (string.IsNullOrWhiteSpace(name))
			{
				return BadRequest("Не указано имя для фильтрации");
			}

			var result = await _roleLogic.ReadListByNameContainsAsync(new RoleSearchModel
			{
				Name = name
			});

			return Ok(result);
		}

		[AuthorizeDocument]
		[HttpGet("paged")]
		public async Task<IActionResult> GetPaged(
			[FromQuery] int pageNumber = 1,
			[FromQuery] int pageSize = 20)
		{
			_logger.LogInformation("Получение ролей с пагинацией");
			var result = await _roleLogic.ReadPagedListAsync(new RoleSearchModel
			{
				PageNumber = pageNumber,
				PageSize = pageSize
			});

			return Ok(result);
		}
	}
}
