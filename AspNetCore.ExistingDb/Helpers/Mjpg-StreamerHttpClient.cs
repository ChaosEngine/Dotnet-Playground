using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb
{
	public interface IMjpgStreamerHttpClient
	{
		Task<FileContentResult> GetLiveImage(CancellationToken token);
	}

	public class MjpgStreamerHttpClientHandler : HttpClientHandler
	{
		public static string Address { get; set; }

		public MjpgStreamerHttpClientHandler(IConfiguration configuration)
		{
			CheckCertificateRevocationList = false;
			ClientCertificateOptions = ClientCertificateOption.Manual;
			ServerCertificateCustomValidationCallback = DangerousAcceptAnyServerCertificateValidator;
			UseCookies = false;

			var addressWithProxy = configuration["LiveWebCamURL"];
			if (!string.IsNullOrEmpty(addressWithProxy))
			{
				var addr_opts = addressWithProxy.Split(';', 2, StringSplitOptions.RemoveEmptyEntries);
				if (addr_opts.Length > 1)
				{
					Proxy = new WebProxy(addr_opts[0], true);
					UseProxy = true;

					Address = addr_opts[1];
				}
				else
					Address = addr_opts[0];

				MjpgStreamerHttpClient.GetContent = MjpgStreamerHttpClient.GetHttpContent;
			}
			else
			{
				MjpgStreamerHttpClient.GetContent = MjpgStreamerHttpClient.GetFileContent;
			}
		}
	}

	/// <summary>
	/// Cached Mjpg-Streamer HttpClient
	/// </summary>
	public class MjpgStreamerHttpClient : IMjpgStreamerHttpClient
	{
		private const string CacheKey = "liveImage";
		public const int LiveImageExpireTimeInSeconds = 1;
		private const int ErrorImageExpireTimeInSeconds = 60 * 60 * 24;
		private const string ErrorImageFileLocalPath = "lib/blueimp-gallery/img/error.png";
		private readonly HttpClient _client;
		private readonly IMemoryCache _cache;
		private readonly IWebHostEnvironment _env;

		internal static Func<HttpClient, IWebHostEnvironment, CancellationToken,
			Task<(DateTime lastModified, byte[] bytes, string contentType, TimeSpan cacheExpiration)>> GetContent;

		public MjpgStreamerHttpClient(HttpClient client, IWebHostEnvironment env, IMemoryCache cache, MjpgStreamerHttpClientHandler handler)
		{
			_client = client;
			_cache = cache;
			_env = env;
		}

		internal static async Task<(DateTime, byte[], string, TimeSpan)> GetFileContent(HttpClient client, IWebHostEnvironment env, CancellationToken token)
		{
			byte[] fetched = await File.ReadAllBytesAsync(Path.Combine(env.WebRootPath, ErrorImageFileLocalPath), token);

			var just_created = (DateTime.UtcNow, fetched, "image/png", TimeSpan.FromDays(ErrorImageExpireTimeInSeconds));
			return just_created;
		}

		internal static async Task<(DateTime, byte[], string, TimeSpan)> GetHttpContent(HttpClient client, IWebHostEnvironment env, CancellationToken token)
		{
			client.BaseAddress = new Uri(MjpgStreamerHttpClientHandler.Address);

			using (HttpResponseMessage resp = await client.GetAsync(client.BaseAddress, token))
			{
				//byte[] fetched = await _client.GetByteArrayAsync(_client.BaseAddress);
				////string str = System.Text.Encoding.Default.GetString(fetched);
				resp.EnsureSuccessStatusCode();
				var fetched = await resp.Content.ReadAsByteArrayAsync();

				var just_created = (DateTime.UtcNow, fetched, MediaTypeNames.Image.Jpeg, TimeSpan.FromSeconds(LiveImageExpireTimeInSeconds));
				return just_created;
			}
		}

		public async Task<FileContentResult> GetLiveImage(CancellationToken token)
		{
			var container = await _cache.GetOrCreateAsync(CacheKey, async (cache_entry) =>
			{
				var cont = await GetContent(_client, _env, token);

				cache_entry.SetAbsoluteExpiration(cont.cacheExpiration);

				return cont;
			});

			return new FileContentResult(container.bytes, container.contentType)
			{
				LastModified = container.lastModified,
			};
		}
	}
}
