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
		private readonly BloggingContext _db;
		private readonly ILoggerFactory _loggerFactory;
		private readonly ILogger<HashesController> _logger;

		public HashesInfo CurrentHashesInfo
		{
			get { return GetHashesInfoFromDB(_db); }
		}

		private HashesInfo GetHashesInfoFromDB(BloggingContext db)
		{
			if (_hashesInfo == null)
			{
				var x = db.HashesInfo.FirstOrDefault();
				_hashesInfo = x;
			}
			return _hashesInfo;
		}

		public HashesController(BloggingContext context, ILoggerFactory loggerFactory, IConfiguration configuration)
		{
			_db = context;
			_db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

			_loggerFactory = loggerFactory;
			_logger = loggerFactory.CreateLogger<HashesController>();
			_configuration = configuration;
		}

		public IActionResult Index()
		{
			if (CurrentHashesInfo == null || (!CurrentHashesInfo.IsCalculating && CurrentHashesInfo.Count <= 0))
			{
				Task.Factory.StartNew((conf) =>
				{
					_logger.LogInformation(0, $"###Starting calculation thread");

					HashesInfo hi = null;
					lock (_locker)
					{
						var bc = new DbContextOptionsBuilder<BloggingContext>();
						bc.UseLoggerFactory(_loggerFactory);
						Startup.ConfigureDBKind(bc, (IConfiguration)conf);

						using (var db = new BloggingContext(bc.Options))
						{
							try
							{
								if (GetHashesInfoFromDB(db) != null)
								{
									_logger.LogInformation(0, $"###Leaving calculation of initial Hash parameters; already present");
									return GetHashesInfoFromDB(db);
								}
								db.Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
								_logger.LogInformation(0, $"###Starting calculation of initial Hash parameters");

								hi = new HashesInfo { ID = 0, IsCalculating = true };

								db.HashesInfo.Add(hi);
								db.SaveChanges(true);
								_hashesInfo = hi;//temporary save to static to indicate calculation and block new calcultion threads

								var alphabet = (from h in db.ThinHashes
												select h.Key.First()
												).Distinct()
												.OrderBy(o => o);
								var count = db.ThinHashes.Count();
								var key_length = db.ThinHashes.Max(x => x.Key.Length);

								hi.Count = count;
								hi.KeyLength = key_length;
								hi.Alphabet = string.Concat(alphabet);
								hi.IsCalculating = false;

								db.Update(hi);
								db.SaveChanges(true);

								db.Database.CommitTransaction();
							}
							catch (Exception)
							{
								db.Database.RollbackTransaction();
								hi = null;
							}
							finally
							{
								_hashesInfo = hi;
								_logger.LogInformation(0, $"###Calculation of initial Hash parameters ended");
							}
						}
					}
					return hi;
				}, _configuration);
			}

			_logger.LogInformation(0,
				$"###Returning {nameof(HashesInfo)}.{nameof(HashesInfo.IsCalculating)} = {(CurrentHashesInfo != null ? CurrentHashesInfo.IsCalculating.ToString() : "null")}");

			ViewBag.Info = CurrentHashesInfo;

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
					ViewBag.Info = CurrentHashesInfo;

					return View(nameof(Index), null);
				}
			}

			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(0, $"{nameof(hi.Search)} = {hi.Search}, {nameof(hi.Kind)} = {hi.Kind.ToString()}");
			});

			hi.Search = hi.Search.Trim().ToLower();

			Task<ThinHashes> found = (from x in _db.ThinHashes
									  where ((hi.Kind == KindEnum.MD5 && x.HashMD5 == hi.Search) || (hi.Kind == KindEnum.SHA256 && x.HashSHA256 == hi.Search))
									  select x)
								 .ToAsyncEnumerable().DefaultIfEmpty(new ThinHashes { Key = "nothing found" }).First();

			if (ajax)
				return Json(new Hashes(await found, hi));
			else
			{
				ViewBag.Info = CurrentHashesInfo;

				return View(nameof(Index), new Hashes(await found, hi));
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
					ViewBag.Info = CurrentHashesInfo;
					return View(nameof(Index), null);
				}
			}
			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(0, $"{nameof(text)} = {text}, {nameof(ajax)} = {ajax.ToString()}");
			});

			text = text.Trim().ToLower();
			Task<List<ThinHashes>> found = null;

			switch (BloggingContext.ConnectionTypeName)
			{
				case "mysqlconnection":
					found = (from x in _db.ThinHashes
							 where (x.HashMD5.StartsWith(text) || x.HashSHA256.StartsWith(text))
							 select x)
							 .ToAsyncEnumerable()
							 .Take(50)
							 .Select(x => new ThinHashes { Key = x.Key, HashMD5 = x.HashMD5, HashSHA256 = x.HashSHA256 })
							 .DefaultIfEmpty(new ThinHashes { Key = "nothing found" })
							 .ToList();
					break;

				case "sqlconnection":
					found = _db.ThinHashes.FromSql(
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
					break;

				case "sqliteconnection":
					found = (from x in _db.ThinHashes
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
				ViewBag.Info = CurrentHashesInfo;
				return View(nameof(Index), await found);
			}
		}
	}
}
