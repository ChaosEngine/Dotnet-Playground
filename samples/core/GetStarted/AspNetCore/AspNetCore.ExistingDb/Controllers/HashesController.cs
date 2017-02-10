using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EFGetStarted.AspNetCore.ExistingDb.Controllers
{
	/// <summary>
	/// TODO: test knockout.js, test ApplicationInsights
	/// </summary>
	public class HashesController : Controller
	{
		private static HashesInfo _hashesInfo;
		private static readonly object _locker = new object();

		private readonly IConfiguration _configuration;
		private readonly BloggingContext _dbaseContext;
		private readonly ILogger<HashesController> _logger;

		public HashesController(BloggingContext context, ILogger<HashesController> logger, IConfiguration configuration)
		{
			_dbaseContext = context;
			_logger = logger;
			_configuration = configuration;
		}

		public IActionResult Index()
		{
			if (_hashesInfo == null || (!_hashesInfo.IsCalculating && _hashesInfo.Count <= 0))
			{
				Task.Factory.StartNew((conf) =>
				{
					_logger.LogInformation(0, $"###Starting calculation thread");

					lock (_locker)
					{
						_logger.LogInformation(0, $"###Starting calculation of initial Hash parameters");

						if (_hashesInfo != null)
						{
							_logger.LogInformation(0, $"###Leaving calculation of initial Hash parameters; already present");
							return _hashesInfo;
						}

						_hashesInfo = new HashesInfo { IsCalculating = true };
						var bc = new DbContextOptionsBuilder<BloggingContext>();
						Startup.ConfigureDBKind(bc, (IConfiguration)conf);

						using (var db = new BloggingContext(bc.Options))
						{
							var alphabet = (from h in db.Hashes
											select h.Key.First()
											).Distinct()
											.OrderBy(o => o);
							var count = db.Hashes.Count();
							var key_length = db.Hashes.Max(x => x.Key.Length);

							_hashesInfo.Count = count;
							_hashesInfo.KeyLength = key_length;
							_hashesInfo.Alphabet = string.Concat(alphabet);
							_hashesInfo.IsCalculating = false;

							_logger.LogInformation(0, $"###Calculation of initial Hash parameters ended");
						}
						ViewBag.Info = _hashesInfo;
					}
					return _hashesInfo;
				}, _configuration);
			}

			_logger.LogInformation(0, $"###Returning {nameof(_hashesInfo)}.{nameof(_hashesInfo.IsCalculating)} = {(_hashesInfo != null ? _hashesInfo.IsCalculating.ToString() : "null")}");

			ViewBag.Info = _hashesInfo;

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
					ViewBag.Info = _hashesInfo;

					return View(nameof(Index), null);
				}
			}

			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(0, $"{nameof(hi.Search)} = {hi.Search}, {nameof(hi.Kind)} = {hi.Kind.ToString()}");
			});

			hi.Search = hi.Search.Trim().ToLower();

			Task<Hashes> found = (from x in _dbaseContext.Hashes
								  where ((hi.Kind == KindEnum.MD5 && x.HashMD5 == hi.Search) || (hi.Kind == KindEnum.SHA256 && x.HashSHA256 == hi.Search))
								  select x)
								 .ToAsyncEnumerable().DefaultIfEmpty(new Hashes { Key = "nothing found" }).First();

			if (ajax)
				return Json(await found);
			else
			{
				ViewBag.Info = _hashesInfo;

				return View(nameof(Index), await found);
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
					ViewBag.Info = _hashesInfo;
					return View(nameof(Index), null);
				}
			}
			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(0, $"{nameof(text)} = {text}, {nameof(ajax)} = {ajax.ToString()}");
			});

			text = text.Trim().ToLower();

			Task<List<ThinHashes>> found = null;
			//var key_pro_name = _dbaseContext.Model.FindEntityType(typeof(Hashes)).FindProperty("Key").GetAnnotations().FirstOrDefault(x => x.Name == "Key").Value;
			switch (BloggingContext.ConnectionTypeName)
			{
				case "mysqlconnection":
					/*found = _dbaseContext.Hashes.FromSql(
$@"SELECT x.SourceKey, x.{nameof(Hashes.HashMD5)}, x.{nameof(Hashes.HashSHA256)}
FROM {nameof(Hashes)} AS x
WHERE x.{nameof(Hashes.HashMD5)} like CAST(@text AS CHAR CHARACTER SET utf8)
UNION ALL
SELECT y.SourceKey, y.{nameof(Hashes.HashMD5)}, y.{nameof(Hashes.HashSHA256)}
FROM {nameof(Hashes)} AS y
WHERE y.{nameof(Hashes.HashSHA256)} like CAST(@text AS CHAR CHARACTER SET utf8)
LIMIT 20",
					new MySqlParameter
					{
						ParameterName = "@text",
						Value = text + '%'
					})
					.ToAsyncEnumerable()
					.Select(x => new ThinHashes { Key = x.Key, HashMD5 = x.HashMD5, HashSHA256 = x.HashSHA256 })
					.DefaultIfEmpty(new ThinHashes { Key = "nothing found" })
					.ToList();*/
					found = (from x in _dbaseContext.Hashes
							 where (x.HashMD5.StartsWith(text) || x.HashSHA256.StartsWith(text))
							 select x)
							 .ToAsyncEnumerable()
							 .Take(50)
							 .Select(x => new ThinHashes { Key = x.Key, HashMD5 = x.HashMD5, HashSHA256 = x.HashSHA256 })
							 .DefaultIfEmpty(new ThinHashes { Key = "nothing found" })
							 .ToList();
					break;

				case "sqlconnection":
					found = _dbaseContext.Hashes.FromSql(
$@"SELECT TOP 20 * FROM (
	SELECT x.[{nameof(Hashes.Key)}], x.{nameof(Hashes.HashMD5)}, x.{nameof(Hashes.HashSHA256)}
	FROM {nameof(Hashes)} AS x
	WHERE x.{nameof(Hashes.HashMD5)} like cast(@text as varchar)
	UNION ALL
	SELECT y.[{nameof(Hashes.Key)}], y.{nameof(Hashes.HashMD5)}, y.{nameof(Hashes.HashSHA256)}
	FROM {nameof(Hashes)} AS y
	WHERE y.{nameof(Hashes.HashSHA256)} like cast(@text as varchar)
) a", new SqlParameter("@text", text + '%'))
						.ToAsyncEnumerable()
						.Select(x => new ThinHashes { Key = x.Key, HashMD5 = x.HashMD5, HashSHA256 = x.HashSHA256 })
						.DefaultIfEmpty(new ThinHashes { Key = "nothing found" })
						.ToList();

					/*found = (from x in _dbaseContext.Hashes
							 where (x.HashMD5.StartsWith(text.Trim()) || x.HashSHA256.StartsWith(text.Trim()))
							 select x)
							 .ToAsyncEnumerable()
							.Take(50)
							.Select(x => new ThinHashes { Key = x.Key, HashMD5 = x.HashMD5, HashSHA256 = x.HashSHA256 })
							.DefaultIfEmpty(new ThinHashes { Key = "nothing found" })
							.ToList();*/
					break;

				case "sqliteconnection":
					found = (from x in _dbaseContext.Hashes
							 where (x.HashMD5.StartsWith(text) || x.HashSHA256.StartsWith(text))
							 select x)
							 .ToAsyncEnumerable()
							 .Take(50)
							 .Select(x => new ThinHashes { Key = x.Key, HashMD5 = x.HashMD5, HashSHA256 = x.HashSHA256 })
							 .DefaultIfEmpty(new ThinHashes { Key = "nothing found" })
							 .ToList();
					break;
				default:
					throw new NotSupportedException($"Bad {nameof(BloggingContext.ConnectionTypeName)} name");
			}

			if (ajax)
				return Json(await found);
			else
			{
				ViewBag.Info = _hashesInfo;
				return View(nameof(Index), await found);
			}
		}
	}
}
