﻿using AspNetCore.ExistingDb.Repositories;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EFGetStarted.AspNetCore.ExistingDb.Controllers
{
	public interface IHashesDataTableController : IDisposable
	{
		IActionResult Index();
		Task<IActionResult> Load(HashesDataTableLoadInput input);
	}

	public class HashesDataTableController : BaseController<ThinHashes>, IHashesDataTableController
	{
		public const string ASPX = "HashesDataTable";

		private readonly ILogger<HashesDataTableController> _logger;
		private readonly IHashesRepositoryPure _repo;
		//private static readonly JsonSerializerSettings _serializationSettings =
		//	new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore, Formatting = Formatting.None };

		public HashesDataTableController(IHashesRepositoryPure repo, ILogger<HashesDataTableController> logger) : base()
		{
			_logger = logger;
			_repo = repo;
			_repo.SetReadOnly(true);
		}

		[HttpGet(ASPX)]
		public virtual IActionResult Index()
		{
			string view_name = "Views/Hashes/HashesDataTable.cshtml";
			return View(view_name);
		}

		[HttpGet]
		public async Task<IActionResult> Load(HashesDataTableLoadInput input)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			CancellationToken token = HttpContext.RequestAborted;
			try
			{
				//await Task.Delay(2_000, token);

				var found = await _repo.PagedSearchAsync(input.Sort, input.Order, input.Search, input.Offset, input.Limit, token);

				var result = new
				{
					total = found.Count,
					rows = found.Itemz//.Select(x => new string[] { x.Key, x.HashMD5, x.HashSHA256 })
				};

				if (input.ExtraParam == "cached" && found.Itemz.Count() > 0)
				{
					HttpContext.Response.GetTypedHeaders().CacheControl =
						new Microsoft.Net.Http.Headers.CacheControlHeaderValue
						{
							Public = true,
							MaxAge = HashesRepository.HashesInfoExpirationInMinutes
						};
				}

				return Json(result/*, _serializationSettings*/);
			}
			catch (OperationCanceledException ex)
			{
				_logger.LogWarning(ex, $"!!!!!!!!!!!!!!!Cancelled {nameof(Load)}::{nameof(_repo.SearchAsync)}" +
					$"({input.Sort}, {input.Order}, {input.Search}, {input.Offset}, {input.Limit}, {token})");
				return Ok();
			}
			catch (Exception)
			{
				throw;
			}
		}

		[HttpGet]
		public async Task<IActionResult> Stream(HashesDataTableLoadInput input)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			CancellationToken token = HttpContext.RequestAborted;
			try
			{
				//await Task.Delay(2_000, token);

				var found = await _repo.PagedSearchAsync(input.Sort, input.Order, input.Search, input.Offset, input.Limit, token);
				var sb = new StringBuilder(found.Count * 113).AppendFormat("{{ \"total\": {0} }}", found.Count);
				//string separator = string.Empty;
				foreach (var item in found.Itemz)
				{
					sb.AppendFormat("{{ \"arr\": [\"{0}\",\"{1}\",\"{2}\"] }}", item[0], item[1], item[2]/*, separator*/);
					//separator = ";";
				}
				string result = sb.ToString();

				if (input.ExtraParam == "cached" && found.Itemz.Count() > 0)
				{
					HttpContext.Response.GetTypedHeaders().CacheControl =
						new Microsoft.Net.Http.Headers.CacheControlHeaderValue
						{
							Public = true,
							MaxAge = HashesRepository.HashesInfoExpirationInMinutes
						};
				}

				return Content(result, "text/plain");
			}
			catch (OperationCanceledException ex)
			{
				_logger.LogWarning(ex, $"!!!!!!!!!!!!!!!Cancelled {nameof(Load)}::{nameof(_repo.SearchAsync)}" +
					$"({input.Sort}, {input.Order}, {input.Search}, {input.Offset}, {input.Limit}, {token})");
				return Ok();
			}
			catch (Exception)
			{
				throw;
			}
		}
	}
}
