﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Lib.AspNetCore.ServerTiming;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public abstract class AnnualMovieGeneratorValidatorModel : PageModel
	{
		public bool EnableAnnualMovieGenerator
		{
			get
			{
				return (base.User != null && base.User.IsInRole("Administrator"))
					&& System.IO.File.Exists("client_secrets.json");
			}
		}
	}

	public class WebCamGallery : AnnualMovieGeneratorValidatorModel
	{
		public const string ASPX = "WebCamGallery";
		private readonly string _imageDirectory;
		private readonly IServerTiming _serverTiming;

		public IEnumerable<FileInfo> ThumbnailJpgs { get; private set; }

		public string BaseWebCamURL { get; }

		public string LiveWebCamURL { get; }

		public string YouTubePlaylistId { get; }

		public Stopwatch Watch { get; }

		public WebCamGallery(IConfiguration configuration, IServerTiming serverTiming)
		{
			Watch = new Stopwatch();
			Watch.Start();

			_imageDirectory = configuration["ImageDirectory"];
			_serverTiming = serverTiming;

			BaseWebCamURL = configuration["BaseWebCamURL"];
			LiveWebCamURL = configuration["LiveWebCamURL"];

			YouTubePlaylistId = configuration["YouTubeAPI:playlistId"];
		}

		public void OnGet()
		{
			_serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("ctor", Watch.ElapsedMilliseconds, "from ctor till GET"));
			Watch.Restart();

			var di = new DirectoryInfo(_imageDirectory);
			if (di.Exists)
			{
				ThumbnailJpgs = di.EnumerateFiles("thumbnail*.jpg", SearchOption.TopDirectoryOnly)
					.OrderByDescending(f => f.LastWriteTime);

				if (false == User.Identity.IsAuthenticated)
				{
					FileInfo img = ThumbnailJpgs.FirstOrDefault();
					if (img != null)
					{
						var expire_date = img.LastWriteTime.AddMinutes(10);
						TimeSpan max_age = expire_date.Subtract(DateTime.Now);
						if (max_age > TimeSpan.Zero)
						{
							Response.GetTypedHeaders().CacheControl =
								new Microsoft.Net.Http.Headers.CacheControlHeaderValue
								{
									Public = true,
									MaxAge = max_age
								};
						}
						//before
						// Response.Headers[HeaderNames.CacheControl] =
						// 	$"public,expires={expire_date.ToUniversalTime().ToString("R")}";
					}
				}
			}

			_serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("READY", Watch.ElapsedMilliseconds, "GET ready"));
		}
	}
}
