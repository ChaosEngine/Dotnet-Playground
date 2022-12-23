using DotnetPlayground.Repositories;
using DotnetPlayground.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace DotnetPlayground.Controllers
{
	public interface IHashesDataTableController : IDisposable
	{
		IActionResult Index();
		Task<IActionResult> Load(HashesDataTableLoadInput input);
	}

	public abstract class HashesDataTableController : BaseController<ThinHashes>, IHashesDataTableController
	{
		//public const string ASPX = "HashesDataTable";

		private readonly ILogger<HashesDataTableController> _logger;
		private readonly IMemoryCache _cache;
		private readonly IHashesRepositoryPure _repo;

		public HashesDataTableController(IHashesRepositoryPure repo, ILogger<HashesDataTableController> logger,
			IMemoryCache cache) : base()
		{
			_logger = logger;
			_cache = cache;
			_repo = repo;
			_repo.SetReadOnly(true);
		}

		public abstract IActionResult Index();

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
				(IEnumerable<ThinHashes> Itemz, int Count) found;
				
				//local cache of initial first items loaded by ServiceWorker call
				if (input.Offset == 0 && input.Limit == 50 &&
					string.IsNullOrEmpty(input.Sort) && string.IsNullOrEmpty(input.Order) &&
					string.IsNullOrEmpty(input.Search) && input.ExtraParam == "cached")
				{
					found = await _cache.GetOrCreateAsync("initial", async (entry) =>
					{
						var result = await _repo.PagedSearchAsync(input.Sort, input.Order, input.Search, input.Offset, input.Limit, token);

						if (result.Count <= 0)
							entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);//no data, ensure to refresh soon
						else
							entry.SlidingExpiration = TimeSpan.FromHours(1);//data exists, cache absolute for longer period

						return result;
					});
				}
				else
				{
					found = await _repo.PagedSearchAsync(input.Sort, input.Order, input.Search, input.Offset, input.Limit, token);
				}

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
				_logger.LogWarning(ex,
					"!!!!!!!!!!!!!!!Cancelled Load::PagedSearchAsync({Sort}, {Order}, {Search}, {Offset}, {Limit}, {token})",
					input.Sort, input.Order, input.Search, input.Offset, input.Limit, token);
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
					sb.AppendFormat("{{ \"arr\": [Key:\"{0}\",HashMD5:\"{1}\",HashSHA256:\"{2}\"] }}", item.Key, item.HashMD5, item.HashSHA256/*, separator*/);
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
				_logger.LogWarning(ex,
					"!!!!!!!!!!!!!!!Cancelled Load::PagedSearchAsync({Sort}, {Order}, {Search}, {Offset}, {Limit}, {token})",
					input.Sort, input.Order, input.Search, input.Offset, input.Limit, token);
				return Ok();
			}
			catch (Exception)
			{
				throw;
			}
		}
	}
}
