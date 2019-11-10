using AspNetCore.ExistingDb.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EFGetStarted.AspNetCore.ExistingDb.Controllers
{
	public class VirtualScrollController : HashesDataTableController
	{
		public new const string ASPX = "VirtualScroll";

		public VirtualScrollController(IHashesRepositoryPure repo, ILogger<HashesDataTableController> logger) : base(repo, logger)
		{
		}

		[HttpGet(ASPX)]
		public override IActionResult Index()
		{
			string view_name = "Views/Hashes/VirtualScroll.cshtml";
			return View(view_name);
		}
	}
}
