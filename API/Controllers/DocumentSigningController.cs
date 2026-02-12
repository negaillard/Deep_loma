using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	public class DocumentSigningController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
