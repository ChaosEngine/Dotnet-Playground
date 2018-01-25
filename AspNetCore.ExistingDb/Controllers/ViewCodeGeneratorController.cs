using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EFGetStarted.AspNetCore.ExistingDb.Controllers
{
	[Route("[controller]")]
	public sealed class ViewCodeGeneratorController : Controller
	{
		public static string CompiledViewCode { get; set; }

		public IActionResult Index()
		{
			ViewData["CompiledViewCode"] = CompiledViewCode;

			return View();
		}

		[HttpPost("[action]")]
		[ValidateAntiForgeryToken]
		public IActionResult CompiledContent(string text)
		{
			if (!ModelState.IsValid)
			{
				return View(nameof(Index), null);
			}

			return Json(CompiledViewCode ?? "");
		}
	}
}
