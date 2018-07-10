using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace EFGetStarted.AspNetCore.ExistingDb
{
	/*public class MjpgStreamerDelegatingHandler : DelegatingHandler
	{
		public MjpgStreamerDelegatingHandler()
		{
		}

		public MjpgStreamerDelegatingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
		{
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return base.SendAsync(request, cancellationToken);
		}
	}*/

	public class MjpgStreamerHttpClientHandler : HttpClientHandler
	{
		public MjpgStreamerHttpClientHandler()
		{
			CheckCertificateRevocationList = false;
			ClientCertificateOptions = ClientCertificateOption.Automatic;
			//ServerCertificateCustomValidationCallback = CertValidator;
			UseCookies = false;
		}

		private bool CertValidator(HttpRequestMessage httpRequestMessage, X509Certificate2 cert,
			X509Chain cetChain, SslPolicyErrors policyErrors)
		{
			return true;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var resp = base.SendAsync(request, cancellationToken);
			return resp;
		}
	}

	/// <summary>
	///  Cached Mjpg-Streamer HttpClient
	/// </summary>
	public class MjpgStreamerHttpClient
	{
		private const string CacheKey = "liveImage";
		private const int ExpireTimeInSeconds = 15;

		private HttpClient _client;
		private IMemoryCache _cache;

		public MjpgStreamerHttpClient(HttpClient client, IConfiguration configuration, IMemoryCache cache)
		{
			var addr = configuration["BaseWebCamURL"] + "/live";

			client.BaseAddress = new Uri(addr);

			_client = client;
			_cache = cache;
		}

		public async Task<(DateTime date, byte[] bytes)> GetLiveImage()
		{
			//if (_cache.TryGetValue(Key, out (DateTime date, byte[] bytes) container))
			//{
			//	return container;
			//}
			//else
			//{
			//	byte[] fetched = await _client.GetByteArrayAsync(_client.BaseAddress);

			//	container = (DateTime.UtcNow, fetched);
			//	_cache.Set(Key, container, TimeSpan.FromSeconds(ExpireTimeInSeconds));

			//	return container;
			//}


			var container = await _cache.GetOrCreateAsync(CacheKey, async (cache_entry) =>
			{
				cache_entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(ExpireTimeInSeconds));

				byte[] fetched = await _client.GetByteArrayAsync(_client.BaseAddress);
				var just_created = (DateTime.UtcNow, fetched);

				return just_created;
			});

			return container;
		}
	}
}
