using DotnetPlayground.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DotnetPlayground.Controllers
{
	public class VirtualScrollController : HashesDataTableController
	{
		public const string ASPX = "VirtualScroll";

		public VirtualScrollController(IHashesRepositoryPure repo, ILogger<HashesDataTableController> logger,
			IMemoryCache cache) : base(repo, logger, cache)
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
