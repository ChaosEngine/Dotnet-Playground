using AspNetCore.ExistingDb.Repositories;
using AspNetCore.ExistingDb.Services;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace EFGetStarted.AspNetCore.ExistingDb.Controllers
{
	public interface IHashesController : IDisposable
	{
		Task<ActionResult> Autocomplete([Required] string text, bool ajax);
		Task<IActionResult> Index();
		Task<ActionResult> Search(HashInput hi, bool ajax);
	}

	public class HashesController : Controller, IHashesController
	{
		public const string ASPX = "Hashes";

		private static readonly object _locker = new object();
		private readonly IBackgroundTaskQueue _backgroundTaskQueue;
		private readonly IHashesRepositoryPure _repo;
		private readonly ILogger<HashesController> _logger;

		public HashesController(IHashesRepositoryPure repo, ILogger<HashesController> logger, IBackgroundTaskQueue backgroundTaskQueue)
		{
			_repo = repo;
			_repo.SetReadOnly(true);

			_logger = logger;
			_backgroundTaskQueue = backgroundTaskQueue;
		}

		public async Task<IActionResult> Index()
		{
			var curr_has_inf = await _repo.CurrentHashesInfo;

			if (curr_has_inf == null || (!curr_has_inf.IsCalculating && curr_has_inf.Count <= 0))
			{
				_backgroundTaskQueue.QueueBackgroundWorkItem(new CalculateHashesInfoBackgroundOperation());
			}

			_logger.LogInformation(0,
				$"###Returning {nameof(HashesInfo)}.{nameof(HashesInfo.IsCalculating)} = {(curr_has_inf != null ? curr_has_inf.IsCalculating.ToString() : "null")}");

			ViewBag.Info = curr_has_inf;

			return View("Index");
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

			var found = await _repo.SearchAsync(hi);

			if (ajax)
				return Json(new Hashes(found, hi));
			else
			{
				ViewBag.Info = await _repo.CurrentHashesInfo;

				return View(nameof(Index), new Hashes(found, hi));
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
