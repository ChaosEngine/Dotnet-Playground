using AspNetCore.ExistingDb.Repositories;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EFGetStarted.AspNetCore.ExistingDb.Controllers
{
	/// <summary>
	/// TODO: test knockout.js, test ApplicationInsights
	/// </summary>
	public class HashesController : Controller
	{
		private static readonly object _locker = new object();
		private readonly IConfiguration _configuration;
		private readonly IHashesRepository _repo;
		private readonly ILoggerFactory _loggerFactory;
		private readonly ILogger<HashesController> _logger;

		public HashesController(IHashesRepository repo, ILoggerFactory loggerFactory, IConfiguration configuration)
		{
			_repo = repo;
			_repo.SetReadOnly(true);

			_loggerFactory = loggerFactory;
			_logger = loggerFactory.CreateLogger<HashesController>();
			_configuration = configuration;
		}

		public async Task<IActionResult> Index()
		{
			var curr_has_inf = await _repo.CurrentHashesInfo;

			if (curr_has_inf == null || (!curr_has_inf.IsCalculating && curr_has_inf.Count <= 0))
			{
				var task = Task.Factory.StartNew((conf) =>
				{
					_logger.LogInformation(0, $"###Starting calculation thread");

					HashesInfo hi = null;
					lock (_locker)
					{
						hi = _repo.CalculateHashesInfo(_loggerFactory, _logger, (IConfiguration)conf);
					}
					return hi;
				}, _configuration);
			}

			_logger.LogInformation(0,
				$"###Returning {nameof(HashesInfo)}.{nameof(HashesInfo.IsCalculating)} = {(curr_has_inf != null ? curr_has_inf.IsCalculating.ToString() : "null")}");

			ViewBag.Info = curr_has_inf;

			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Search(HashInput hi, bool ajax)
		{
			if (!ModelState.IsValid)
			{
				if (ajax)
					return Json("error");
				else
				{
					ViewBag.Info = await _repo.CurrentHashesInfo;

					return View(nameof(Index), null);
				}
			}

			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(0, $"{nameof(hi.Search)} = {hi.Search}, {nameof(hi.Kind)} = {hi.Kind.ToString()}");
			});

			hi.Search = hi.Search.Trim().ToLower();

			var found = _repo.FindByAsync(x => ((hi.Kind == KindEnum.MD5 && x.HashMD5 == hi.Search) || (hi.Kind == KindEnum.SHA256 && x.HashSHA256 == hi.Search)));

			if (ajax)
				return Json(new Hashes((await found).FirstOrDefault(), hi));
			else
			{
				ViewBag.Info = await _repo.CurrentHashesInfo;

				return View(nameof(Index), new Hashes((await found).FirstOrDefault(), hi));
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Autocomplete([Required]string text, bool ajax)
		{
			if (!ModelState.IsValid)
			{
				if (ajax)
					return Json("error");
				else
				{
					ViewBag.Info = await _repo.CurrentHashesInfo;
					return View(nameof(Index), null);
				}
			}
			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(0, $"{nameof(text)} = {text}, {nameof(ajax)} = {ajax.ToString()}");
			});

			text = text.Trim().ToLower();

			var found = _repo.AutoComplete(text);

			if (ajax)
				return Json(await found);
			else
			{
				ViewBag.Info = await _repo.CurrentHashesInfo;
				return View(nameof(Index), await found);
			}
		}
	}
}
