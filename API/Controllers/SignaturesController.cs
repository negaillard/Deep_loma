using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	public class SignaturesController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
