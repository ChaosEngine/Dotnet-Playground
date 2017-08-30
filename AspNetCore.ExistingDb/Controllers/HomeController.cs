using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace EFGetStarted.AspNetCore.ExistingDb.Controllers
{
	public class HomeController : Controller
	{
		class RandomData
		{
			private static HashAlgorithm _hasher;

			public byte[] Buffer { get; set; }

			public RandomData()
			{
			}

			public RandomData(int size)
			{
				var rnd = new Random(Environment.TickCount);
				Buffer = new byte[size];

				rnd.NextBytes(Buffer);
			}

			public RandomData(int randomMinLength, int randomMaxLength)
			{
				var rnd = new Random(Environment.TickCount);
				var size = rnd.Next(randomMinLength, randomMaxLength);
				Buffer = new byte[size];

				rnd.NextBytes(Buffer);
			}

			public static string RandomStr()
			{
				string rStr = Path.GetRandomFileName();
				rStr = rStr.Replace(".", ""); // For Removing the .
				return rStr;
			}

			public override string ToString()
			{
				if (_hasher == null)
					_hasher = SHA256.Create();

				string hash_str = null;
				if (Buffer != null && Buffer.Length > 0)
					hash_str = BitConverter.ToString(_hasher.ComputeHash(Buffer)).Replace("-", "").ToLowerInvariant();
				return hash_str;
			}
		}

		const string SessionKeyName = "_Name";
		const string SessionKeyYearsMember = "_YearsMember";
		const string SessionKeyDate = "_Date";

		private readonly IConfiguration _configuration;
		private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
		{
			_configuration = configuration;
			_logger = logger;
		}

		public async Task<IActionResult> Index()
		{
			if (!HttpContext.Session.Keys.Contains(SessionKeyName))
				HttpContext.Session.SetString(SessionKeyName, RandomData.RandomStr());
			if (!HttpContext.Session.Keys.Contains(SessionKeyYearsMember))
				HttpContext.Session.SetInt32(SessionKeyYearsMember, 3);
			if (!HttpContext.Session.Keys.Contains(SessionKeyDate))
				HttpContext.Session.Set(SessionKeyDate, DateTime.Now);
			if (!HttpContext.Session.Keys.Contains(typeof(RandomData).Name))
				HttpContext.Session.Set(typeof(RandomData).Name, new RandomData(500, 8000));

			return await Task.FromResult(View());
		}

		public async Task<IActionResult> About()
		{
			ViewData["Message"] = "Your application description page.";

			return await Task.FromResult(View());
		}

		public async Task<IActionResult> Contact()
		{
			var name = HttpContext.Session.GetString(SessionKeyName);
			var yearsMember = HttpContext.Session.GetInt32(SessionKeyYearsMember);

			var date = HttpContext.Session.Get<DateTime>(SessionKeyDate);
			var sessionTime = date.TimeOfDay.ToString();
			var currentTime = DateTime.Now.TimeOfDay.ToString();
			var time = $"Current time: {currentTime} - session time: {sessionTime}";

			var big_blob = HttpContext.Session.Get<RandomData>(typeof(RandomData).Name);

			ViewData["Message"] = $"Name: '{name}'<br />Membership years: '{yearsMember}'<br />time: '{time}'<br />"
				+ $"BigBlob: '{big_blob}'";

			return await Task.FromResult(View());
		}

		[HttpGet]
		public async Task<IActionResult> UnintentionalErr()
		{
			return await Task.FromResult(View(nameof(UnintentionalErr)));
		}

		[HttpPost]
		public async Task<IActionResult> UnintentionalErr(string action)
		{
			switch (action?.Trim()?.ToLowerInvariant())
			{
				case "repost":
					TempData.Add("itwas", $"Clicked something {action}");

					var destination_url = $"{nameof(UnintentionalErr)}";

					return await Task.FromResult(Redirect(destination_url));

				case "exception":
					throw new Exception("test exception");

				default:
					throw new NotSupportedException("action not supported");
			}
		}

		public async Task<IActionResult> Error(int statusCode)
		{
			if (statusCode <= 0)
				statusCode = Response.StatusCode;

			/*switch (Response.StatusCode)
			{
				case StatusCodes.Status404NotFound:
					break;
				default:
					break;
			}*/

			var reExecute = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
			_logger.LogInformation($"Unexpected Status Code: {statusCode}, OriginalPath: {reExecute?.OriginalPath}");

			return await Task.FromResult(View(statusCode));
		}

		[HttpPost]
		public async Task<IActionResult> ClientsideLog(LogLevel? level, string message, string url, string line, string col, string error)
		{
			switch (level.GetValueOrDefault(LogLevel.None))
			{
				case LogLevel.Trace:
					_logger.LogTrace(message);
					break;
				case LogLevel.Debug:
					_logger.LogDebug(message);
					break;
				case LogLevel.Information:
					_logger.LogInformation(message);
					break;
				case LogLevel.Warning:
					_logger.LogWarning(message);
					break;
				case LogLevel.Error:
					_logger.LogError(message);
					break;
				default:
					break;
			}
			var ok = Ok();
			return await Task.FromResult(ok);
		}

		[HttpGet("Home/UnintentionalErr/sleep")]
		//[ValidateAntiForgeryToken]
		public string GetSleep()
		{
			try
			{
				var token = HttpContext.RequestAborted;
				token.ThrowIfCancellationRequested();

				if (token.IsCancellationRequested)
				{
					_logger.LogWarning("Aborted0");
					return "0";
				}

				Thread.Sleep(2_000);

				if (token.IsCancellationRequested)
				{
					_logger.LogWarning("Aborted1");
					return "0";
				}

				return Process.GetCurrentProcess().Threads.Count.ToString();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex.StackTrace);
				throw;
			}
		}

		[HttpGet("Home/UnintentionalErr/delay")]
		//[ValidateAntiForgeryToken]
		public async Task<string> GetDelay()
		{
			try
			{
				var token = HttpContext.RequestAborted;
				token.ThrowIfCancellationRequested();

				if (token.IsCancellationRequested)
				{
					_logger.LogWarning("Aborted0");
					return "0";
				}

				await Task.Delay(2_000, token);

				if (token.IsCancellationRequested)
				{
					_logger.LogWarning("Aborted1");
					return "0";
				}

				return Process.GetCurrentProcess().Threads.Count.ToString();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex.StackTrace);
				throw;
			}
		}
	}
}
