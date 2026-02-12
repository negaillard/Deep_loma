
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	public class DocumentsController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
