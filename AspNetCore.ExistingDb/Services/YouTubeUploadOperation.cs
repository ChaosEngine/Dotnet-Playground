using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.ExistingDb.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspNetCore.ExistingDb.Services
{
	/// <summary>
	/// Youtube video lister and uploader (privately)
	/// </summary>
	/// <seealso cref="AspNetCore.ExistingDb.Services.BackgroundOperationBase" />
	public class YouTubeUploadOperation : BackgroundOperationBase
	{
		private readonly string _videoFileNameToUpload;
		private readonly string _clientSecretsJsonFileName;

		/// <summary>
		/// Initializes a new instance of the <see cref="YouTubeUploadOperation" /> class.
		/// </summary>
		/// <param name="videoFileNameToUpload">The video file name to upload.</param>
		/// <param name="clientSecretsJsonFileName">YouTube client secrets json file.</param>
		public YouTubeUploadOperation(string videoFileNameToUpload, string clientSecretsJsonFileName)
		{
			_videoFileNameToUpload = videoFileNameToUpload;
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
				var logger = scope.ServiceProvider.GetRequiredService<ILogger<YouTubeUploadOperation>>();
				conf = scope.ServiceProvider.GetRequiredService<IConfiguration>();

				await ExecuteYouTubeDataApiV3(conf.GetSection("YouTubeAPI"), _clientSecretsJsonFileName,
					GetSharedKeysDir(), logger, token);
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

		private async Task<bool> ExecuteYouTubeDataApiV3(IConfiguration youTubeConf, string clientSecretsJson,
			string sharedSecretFolder, ILogger<YouTubeUploadOperation> logger, CancellationToken token)
		{
			UserCredential credential;

			using (var stream = new FileStream(clientSecretsJson, FileMode.Open, FileAccess.Read))
			{
				FileDataStore store = string.IsNullOrEmpty(sharedSecretFolder) ?
					new FileDataStore(this.GetType().ToString()) : new FileDataStore(sharedSecretFolder, true);

				credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					// This OAuth 2.0 access scope allows for read-only access to the authenticated 
					// user's account, but not other types of account access.
					new[] { YouTubeService.Scope.Youtube, YouTubeService.Scope.YoutubeUpload },
					"user", token, store
					);
			}

			using (var youtubeService = new YouTubeService(new BaseClientService.Initializer
			{
				HttpClientInitializer = credential,
				//ApiKey = configuration["ApiKey"],
				ApplicationName = youTubeConf["ApplicationName"]
			}))
			{
				string new_video_title = $"timelapse {DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
				bool already_uploaded = false;
				var nextPageToken = "";
				while (nextPageToken != null && !already_uploaded)
				{
					var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
					playlistItemsListRequest.PlaylistId = youTubeConf["playlistId"];
					playlistItemsListRequest.Fields = "items(snippet(description,publishedAt,title))";
					playlistItemsListRequest.MaxResults = 10;
					playlistItemsListRequest.PageToken = nextPageToken;
					// Retrieve the list of videos uploaded to the authenticated user's channel.
					var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync(token);

					foreach (var item in playlistItemsListResponse.Items)
					{
						if (item.Snippet.Title == new_video_title)
						{
							already_uploaded = true;
							logger.LogWarning("'{title}' already uploaded aborting", item.Snippet.Title);
							break;
						}
						// Print information about each video.
						logger.LogInformation("'{title}' [{description}] {publishedAt}", item.Snippet.Title, item.Snippet.Description,
							item.Snippet.PublishedAt);
					}

					nextPageToken = playlistItemsListResponse.NextPageToken;
				}

				if (already_uploaded || string.IsNullOrEmpty(_videoFileNameToUpload))
					return false;

				//upload
				var video = new Video
				{
					Snippet = new VideoSnippet
					{
						Title = new_video_title,
						Description = $"Daily timelapse video taken on {DateTime.Now}",
						Tags = new string[] { "timelapse", "video", "webcam" },
						//"1" -> "Film & Animation", see https://developers.google.com/youtube/v3/docs/videoCategories/list
						CategoryId = "1",
					},
					Status = new VideoStatus
					{
						PrivacyStatus = "private" // or "private" or "public"
					}
				};

				string successVideoID = null;
				using (var fileStream = new FileStream(_videoFileNameToUpload, FileMode.Open))
				{
					var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
					videosInsertRequest.ProgressChanged += (IUploadProgress progress) =>
					{
						switch (progress.Status)
						{
							case UploadStatus.Uploading:
								logger.LogInformation("{bytesSent} bytes sent.", progress.BytesSent);
								break;

							case UploadStatus.Failed:
								logger.LogWarning("An error prevented the upload from completing.\n{exception}", progress.Exception);
								break;
						}
					};
					videosInsertRequest.ResponseReceived += (Video uploadedVideo) =>
					{
						successVideoID = uploadedVideo.Id;
						logger.LogInformation("Video id '{id}' was successfully uploaded.", uploadedVideo.Id);
					};

					var uploaded = await videosInsertRequest.UploadAsync(token);

					if (uploaded.Status == UploadStatus.Completed && !string.IsNullOrEmpty(successVideoID))
					{
						var newPlaylistItem = new PlaylistItem();
						newPlaylistItem.Snippet = new PlaylistItemSnippet();
						newPlaylistItem.Snippet.PlaylistId = youTubeConf["playlistId"];
						newPlaylistItem.Snippet.ResourceId = new ResourceId();
						newPlaylistItem.Snippet.ResourceId.Kind = "youtube#video";
						newPlaylistItem.Snippet.ResourceId.VideoId = successVideoID;
						newPlaylistItem = await youtubeService.PlaylistItems.Insert(newPlaylistItem, "snippet")
							.ExecuteAsync(token);

						logger.LogInformation("Video id '{id}' was added to playlist id '{playlistID}'.",
							successVideoID, newPlaylistItem.Snippet.PlaylistId);

						return true;
					}

					return false;
				}//end using fileStream				
			}//end using youtubeService
		}
	}//end class
}
