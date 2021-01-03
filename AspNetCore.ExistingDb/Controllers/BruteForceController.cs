using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.ExistingDb.Controllers
{
	public class BruteForceController : Controller
	{
		[HttpGet]
		public IActionResult Index()
		{
			string view_name = "Views/Hashes/BruteForce.cshtml";
			return View(view_name);
		}
	}
}
