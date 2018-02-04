using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public class WebCamGallery : PageModel
	{
		private readonly string _imageDirectory;

		public IEnumerable<string> Jpgs { get; private set; }

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

				Jpgs = files.OrderByDescending(f => f.LastWriteTime).Select(x => x.Name);
			}
		}
	}
}
