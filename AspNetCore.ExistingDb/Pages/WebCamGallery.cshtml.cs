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
			}
			
			int durationInSeconds = (60 * 60) * (10 - (DateTime.Now.Minute % 10)) + (60 - DateTime.Now.Second);
			HttpContext.Response.Headers[HeaderNames.CacheControl] =
				$"public,max-age={durationInSeconds}, must-revalidate";
		}
	}
}
