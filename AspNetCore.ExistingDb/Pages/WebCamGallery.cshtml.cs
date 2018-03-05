using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lib.AspNetCore.ServerTiming;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public class WebCamGallery : PageModel
	{
		public const string ASPX = "WebCamGallery";
		private readonly string _imageDirectory;
		private readonly IServerTiming _serverTiming;

		public IEnumerable<FileInfo> Jpgs { get; private set; }

		public string LiveWebCamURL { get; private set; }

		public string TimelapsVideoURL { get; private set; }

		public Stopwatch Watch { get; private set; }

		public WebCamGallery(IConfiguration configuration, IServerTiming serverTiming)
		{
			Watch = new Stopwatch();
			Watch.Start();
			
			_imageDirectory = configuration["ImageDirectory"];
			LiveWebCamURL = configuration["LiveWebCamURL"];
			TimelapsVideoURL = configuration["TimelapsVideoURL"];
			_serverTiming = serverTiming;
		}

		public void OnGet()
		{
			_serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("ctor", Watch.ElapsedMilliseconds, "from ctor till GET"));
			Watch.Restart();

			if (Directory.Exists(_imageDirectory))
			{
				var di = new DirectoryInfo(_imageDirectory);
				var files = di.EnumerateFiles("*.jpg", SearchOption.TopDirectoryOnly);

				Jpgs = files.OrderByDescending(f => f.LastWriteTime)/*.Select(x => x.Name)*/;

				if (Jpgs.Any())
				{
					var expire_date = Jpgs.First().LastWriteTime.AddMinutes(10);
					Response.Headers[HeaderNames.CacheControl] =
						$"public,expires={expire_date.ToUniversalTime().ToString("R")}";
				}
			}

			_serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("READY", Watch.ElapsedMilliseconds, "GET ready"));
		}
	}
}
