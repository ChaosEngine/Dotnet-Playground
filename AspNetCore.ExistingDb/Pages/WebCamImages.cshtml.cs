using Lib.AspNetCore.ServerTiming;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mime;

namespace EFGetStarted.AspNetCore.ExistingDb.Models
{
	public class WebCamImagesModel : PageModel
	{
		public const string ASPX = "WebCamImages";

		public IActionResult OnGet([FromServices]IConfiguration configuration, [FromServices]IServerTiming serverTiming, string fileName)
		{
			var watch = new Stopwatch();
			watch.Start();

			string imageDirectory = configuration["ImageDirectory"];
			if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(imageDirectory))
			{
				serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("GET", watch.ElapsedMilliseconds, "error"));
				return NotFound("No image directory present");
			}

			string path = Path.Combine(imageDirectory, fileName);
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
					var etag_str = $"\"{Convert.ToString(etagHash, 16)}\"";

					if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out StringValues incomingEtag)
						&& string.Compare(incomingEtag[0], etag_str) == 0)
					{
						Response.Clear();
						Response.StatusCode = (int)HttpStatusCode.NotModified;
						serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("GET", watch.ElapsedMilliseconds, "NotModified GET"));

						return new StatusCodeResult((int)HttpStatusCode.NotModified);
					}
					var etag = new EntityTagHeaderValue(etag_str);

					//var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
					PhysicalFileResult pfr = base.PhysicalFile(path, MediaTypeNames.Image.Jpeg);
					pfr.EntityTag = etag;
					//pfr.LastModified = lastModified;
					serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("GET", watch.ElapsedMilliseconds, "full file GET"));

					return pfr;
				}
			}
			serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("GET", watch.ElapsedMilliseconds, "not found GET"));
			return NotFound();
		}
	}
}
