using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public class WebCamImagesModel : PageModel
	{
		public IActionResult OnGet([FromServices]IConfiguration configuration, string fileName)
		{
			if (string.IsNullOrEmpty(fileName)) return NotFound();

			var imageDirectory = configuration["ImageDirectory"];
			var path = Path.Combine(imageDirectory, fileName);
			if (System.IO.File.Exists(path))
			{
				if (Path.GetDirectoryName(path) == imageDirectory)
				{
					var fi = new FileInfo(path);
					var length = fi.Length;
					DateTimeOffset last = fi.LastWriteTime;
					// Truncate to the second.
					var lastModified = new DateTimeOffset(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, last.Offset).ToUniversalTime();

					long etagHash = lastModified.ToFileTime() ^ length;
					var etag_str = '\"' + Convert.ToString(etagHash, 16) + '\"';

					string incomingEtag = (Request.Headers as FrameRequestHeaders).HeaderIfNoneMatch;

					if (String.Compare(incomingEtag, etag_str) == 0)
					{
						Response.Clear();
						Response.StatusCode = (int)System.Net.HttpStatusCode.NotModified;
						return new StatusCodeResult((int)System.Net.HttpStatusCode.NotModified);
					}
					var etag = new EntityTagHeaderValue(etag_str);

					//var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
					PhysicalFileResult pfr = base.PhysicalFile(path, "image/jpg");
					pfr.EntityTag = etag;

					return pfr;
				}
			}
			return NotFound();
		}
	}
}
