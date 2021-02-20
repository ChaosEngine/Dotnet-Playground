using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotnetPlayground.Models;
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

namespace DotnetPlayground.Services
{
	/// <summary>
	/// Youtube playlist items dumper
	/// </summary>
	/// <seealso cref="DotnetPlayground.Services.BackgroundOperationBase" />
	public class YouTubePlaylistDumpOperation : BackgroundOperationBase
	{

		public IEnumerable<object[]> Product { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="YouTubePlaylistDumpOperation" /> class.
		/// </summary>
		public YouTubePlaylistDumpOperation()
		{
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
				conf = scope.ServiceProvider.GetRequiredService<IConfiguration>();
				var context = scope.ServiceProvider.GetRequiredService<BloggingContext>();
				var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();


				var year_ago_date = DateTime.Now.AddYears(-1);
				var dest_dir = YouTubeUploadOperation.GetSharedKeysDir(conf);
				var lst = await ExecuteYouTubeDataApiV3(conf.GetSection("YouTubeAPI"), context, env, token);

				var query = lst.OrderBy(o => o.Snippet.PublishedAt);
				Product = query.Select(item => new object[] {
						item.Snippet.Position,
						item.Snippet.Title,
						item.Snippet.ResourceId.VideoId,
						item.Snippet.PublishedAt.GetValueOrDefault()
					})
					.Where(tab => (DateTime)tab[3] >= year_ago_date);

				if (!string.IsNullOrEmpty(dest_dir))
				{
					using (StreamWriter writer = File.CreateText(Path.Combine(dest_dir, "download-archive.txt")))
					{
						foreach (var item in query)
						{
							var date = item.Snippet.PublishedAt.GetValueOrDefault();
							bool is_to_be_deleted = date < year_ago_date;
							if (!is_to_be_deleted)
								await writer.WriteLineAsync($"youtube {item.Snippet.ResourceId.VideoId}");
							else
								await writer.WriteLineAsync($"#youtube {item.Snippet.ResourceId.VideoId} #too old {date:O} - for deletion");

						}
						await writer.FlushAsync();
					}
				}
			}
		}

		private async Task<IEnumerable<PlaylistItem>> ExecuteYouTubeDataApiV3(IConfiguration youTubeConf,
			BloggingContext context, IWebHostEnvironment environment, CancellationToken token)
		{
			UserCredential credential;

			using (var stream = new FileStream(youTubeConf["ClientSecretsFileName"], FileMode.Open, FileAccess.Read))
			{
				var store = new GoogleKeyContextStore(context, environment, token);

				credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					// This OAuth 2.0 access scope allows for read-only access to the authenticated 
					// user's account, but not other types of account access.
					new[] { YouTubeService.Scope.YoutubeReadonly },
					//Below scope is with higher access - use it here only for obtaining new/refresh token (browser access auth required)
					//new[] { YouTubeService.Scope.Youtube, YouTubeService.Scope.YoutubeUpload },
					"user", token, store
					);
			}

			using (var youtubeService = new YouTubeService(new BaseClientService.Initializer
			{
				HttpClientInitializer = credential,
				ApplicationName = youTubeConf["ApplicationName"]
			}))
			{
				var lst = new List<PlaylistItem>(365);
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

					foreach (PlaylistItem item in playlistItemsListResponse.Items)
					{
						lst.Add(item);
					}

					nextPageToken = playlistItemsListResponse.NextPageToken;
				}

				return lst;
			}//end using youtubeService
		}
	}//end class
}
