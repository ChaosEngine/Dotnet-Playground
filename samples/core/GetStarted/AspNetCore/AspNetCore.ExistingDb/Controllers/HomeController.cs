using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace EFGetStarted.AspNetCore.ExistingDb.Controllers
{
	public class HomeController : Controller
	{
		class BigBlob
		{
			private static HashAlgorithm _hasher;

			public byte[] Buffer { get; set; }

			public BigBlob()
			{
			}

			public BigBlob(int size)
			{
				var rnd = new Random(Environment.TickCount);
				Buffer = new byte[size];

				rnd.NextBytes(Buffer);
			}

			public BigBlob(int randomMinLength, int randomMaxLength)
			{
				var rnd = new Random(Environment.TickCount);
				var size = rnd.Next(randomMinLength, randomMaxLength);
				Buffer = new byte[size];

				rnd.NextBytes(Buffer);
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
		const string SessionBigBlog = "_BigBlob";

		public IActionResult Index()
		{
			if (!HttpContext.Session.Keys.Contains(SessionKeyName))
				HttpContext.Session.SetString(SessionKeyName, "Rick");
			if (!HttpContext.Session.Keys.Contains(SessionKeyYearsMember))
				HttpContext.Session.SetInt32(SessionKeyYearsMember, 3);
			if (!HttpContext.Session.Keys.Contains(SessionKeyDate))
				HttpContext.Session.Set(SessionKeyDate, DateTime.Now);
			if (!HttpContext.Session.Keys.Contains(SessionBigBlog))
				HttpContext.Session.Set(SessionBigBlog, new BigBlob(500, 8000));

			return View();
		}

		public IActionResult About()
		{
			ViewData["Message"] = "Your application description page.";

			return View();
		}

		public IActionResult Contact()
		{
			var name = HttpContext.Session.GetString(SessionKeyName);
			var yearsMember = HttpContext.Session.GetInt32(SessionKeyYearsMember);

			var date = HttpContext.Session.Get<DateTime>(SessionKeyDate);
			var sessionTime = date.TimeOfDay.ToString();
			var currentTime = DateTime.Now.TimeOfDay.ToString();
			var time = $"Current time: {currentTime} - session time: {sessionTime}";

			var big_blob = HttpContext.Session.Get<BigBlob>(SessionBigBlog);

			ViewData["Message"] = $"Name: '{name}'<br />Membership years: '{yearsMember}'<br />time: '{time}'<br />"
				+ $"BigBlob: '{big_blob}'";

			return View();
		}

		public IActionResult Error()
		{
			return View();
		}
	}
}
