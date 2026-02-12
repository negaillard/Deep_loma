using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	public class InternalSignaturesController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
