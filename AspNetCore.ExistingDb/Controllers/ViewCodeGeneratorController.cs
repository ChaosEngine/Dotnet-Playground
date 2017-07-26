using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EFGetStarted.AspNetCore.ExistingDb.Controllers
{
	[Route("[controller]")]
	public class ViewCodeGeneratorController : Controller
	{
		public static string CompiledViewCode { get; set; }

		public IActionResult Index()
		{
			ViewData["CompiledViewCode"] = CompiledViewCode;

			return View();
		}

		[HttpPost(nameof(CompiledContent))]
		[ValidateAntiForgeryToken]
		public IActionResult CompiledContent(string text)
		{
			if (!ModelState.IsValid)
			{
				return View(nameof(Index), null);
			}

			return Json(CompiledViewCode ?? "");
		}

		// GET api/values/sleep
		[HttpGet("sleep")]
		public string GetSleep()
		{
			Thread.Sleep(1000);
			return Process.GetCurrentProcess().Threads.Count.ToString();
		}

		// GET api/values/delay
		[HttpGet("delay")]
		async public Task<string> GetDelay()
		{
			await Task.Delay(1000);
			return Process.GetCurrentProcess().Threads.Count.ToString();
		}
	}
}
