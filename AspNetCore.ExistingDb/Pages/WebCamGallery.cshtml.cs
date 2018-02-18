using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public class WebCamGallery : PageModel
	{
		private readonly string _imageDirectory;

		public const string ASPX = "WebCamGallery";

		public IEnumerable<FileInfo> Jpgs { get; private set; }

		public string LiveWebCamURL { get; private set; }

		public string TimelapsVideoURL { get; private set; }

		public WebCamGallery(IConfiguration configuration)
		{
			var imageDirectory = configuration["ImageDirectory"];
			_imageDirectory = imageDirectory;
			LiveWebCamURL = configuration["LiveWebCamURL"];
			TimelapsVideoURL = configuration["TimelapsVideoURL"];
		}

		public void OnGet()
		{
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
		}
	}
}
