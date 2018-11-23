using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Helpers
{
	public static class LogRequestHeadersExtensions
	{
		/// <summary>Logs the request headers.</summary>
		/// <param name="app">The application.</param>
		/// <param name="loggerFactory">The logger factory.</param>
		/// <param name="searchedHeader">The searched header.</param>
		public static void LogRequestHeaders(this IApplicationBuilder app, ILoggerFactory loggerFactory, string searchedHeader)
		{
			var logger = loggerFactory.CreateLogger("Request Headers");

			app.Use(async (context, next) =>
			{
				if (context.Request.Headers.ContainsKey(searchedHeader))
				{
					var builder = new System.Text.StringBuilder(Environment.NewLine);
					foreach (var header in context.Request.Headers)
					{
						builder.AppendLine($"{header.Key}:{header.Value}");
					}
					if (builder.Length > 0)
						logger.LogWarning(builder.ToString());
				}

				await next.Invoke();
			});
		}
	}
}
