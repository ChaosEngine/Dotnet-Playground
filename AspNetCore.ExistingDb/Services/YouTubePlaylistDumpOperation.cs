using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspNetCore.ExistingDb.Services
{
	/// <summary>
	/// Youtube playlist items dumper
	/// </summary>
	/// <seealso cref="AspNetCore.ExistingDb.Services.BackgroundOperationBase" />
	public class YouTubePlaylistDumpOperation : BackgroundOperationBase
	{
		private readonly DateTime _sinceWhen;
		private readonly string _clientSecretsJsonFileName;

		public IEnumerable<object[]> Product { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="YouTubePlaylistDumpOperation" /> class.
		/// </summary>
		/// <param name="sinceWhen">Since when take videos.</param>
		/// <param name="clientSecretsJsonFileName">YouTube client secrets json file.</param>
		public YouTubePlaylistDumpOperation(DateTime sinceWhen, string clientSecretsJsonFileName)
		{
			_sinceWhen = sinceWhen;
			_clientSecretsJsonFileName = clientSecretsJsonFileName;
		}

		/// <summary>
		/// Does the work asynchronous.
		/// </summary>
		/// <param name="services">The services.</param>
		/// <param name="token">The token.</param>
		/// <returns>completion task</returns>
		public async override Task DoWorkAsync(IServiceProvider services, CancellationToken token)
		{
			IConfiguration conf;
			using (IServiceScope scope = services.CreateScope())
			{
				var logger = scope.ServiceProvider.GetRequiredService<ILogger<YouTubePlaylistDumpOperation>>();
				conf = scope.ServiceProvider.GetRequiredService<IConfiguration>();
				var context = scope.ServiceProvider.GetRequiredService<BloggingContext>();
				var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();


				var year_ago = DateTime.Now.AddYears(-1);
				var dest_dir = GetSharedKeysDir();
				var tmp = await ExecuteYouTubeDataApiV3(conf.GetSection("YouTubeAPI"), _clientSecretsJsonFileName,
					dest_dir, logger, context, env, token);

				Product = tmp.Where(tab => ((DateTime)tab[3]) >= year_ago)
					.OrderBy(o => (DateTime)o[3])
					.ToArray();
				
				if (!string.IsNullOrEmpty(dest_dir))
				{
					using (StreamWriter writer = File.CreateText(Path.Combine(dest_dir, "download-archive.txt")))
					{
						foreach (var line in Product)
							await writer.WriteLineAsync($"youtube {line[2]}");
						await writer.FlushAsync();
					}
				}
			}


			//private function
			string GetSharedKeysDir()
			{
				var sharedKeysDir = conf["SharedKeysDirectory"]?.Replace('/', Path.DirectorySeparatorChar)
					?.Replace('\\', Path.DirectorySeparatorChar);
				if (string.IsNullOrEmpty(sharedKeysDir) || !Directory.Exists(sharedKeysDir))
					sharedKeysDir = null;

				return sharedKeysDir;
			}
		}

		private async Task<IEnumerable<object[]>> ExecuteYouTubeDataApiV3(IConfiguration youTubeConf, string clientSecretsJson,
			string sharedSecretFolder, ILogger<YouTubePlaylistDumpOperation> logger, BloggingContext context,
			IWebHostEnvironment environment, CancellationToken token)
		{
			UserCredential credential;

			using (var stream = new FileStream(clientSecretsJson, FileMode.Open, FileAccess.Read))
			{
				var store = new EFContextDataStore<BloggingContext>(context, environment, token);

				credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					// This OAuth 2.0 access scope allows for read-only access to the authenticated 
					// user's account, but not other types of account access.
					new[] { YouTubeService.Scope.YoutubeReadonly },
					"user", token, store
					);
			}

			using (var youtubeService = new YouTubeService(new BaseClientService.Initializer
			{
				HttpClientInitializer = credential,
				ApplicationName = youTubeConf["ApplicationName"]
			}))
			{
				var lst = new List<object[]>(365);
				var nextPageToken = "";
				while (nextPageToken != null)
				{
					var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
					playlistItemsListRequest.PlaylistId = youTubeConf["playlistId"];
					// playlistItemsListRequest.Fields = "items(snippet(title,position,resourceId(videoId)))";
					playlistItemsListRequest.MaxResults = 50;
					playlistItemsListRequest.PageToken = nextPageToken;
					// Retrieve the list of videos uploaded to the authenticated user's channel.
					var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync(token);

					foreach (var item in playlistItemsListResponse.Items)
					{
						lst.Add(new object[] {
							item.Snippet.Position,
							item.Snippet.Title,
							item.Snippet.ResourceId.VideoId,
							item.Snippet.PublishedAt.GetValueOrDefault(DateTime.MinValue)//.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
						});
					}

					nextPageToken = playlistItemsListResponse.NextPageToken;
				}

				return lst;
			}//end using youtubeService
		}
	}//end class
}
