using System;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.ExistingDb.Services;

namespace AspNetCore.ExistingDb.Services
{
	public class YouTubeUploadOperation : BackgroundOperationBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="YouTubeUploadOperation" /> class.
		/// </summary>
		/// <param name="videoFileNameToUpload">The video file name to upload.</param>
		public YouTubeUploadOperation(string videoFileNameToUpload,
			string youTubeUserAcountID, string youTubeUserCredentials, string youTubeChannelName)
		{
		}

		/// <summary>
		/// Does the work asynchronous.
		/// </summary>
		/// <param name="services">The services.</param>
		/// <param name="token">The token.</param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public override Task DoWorkAsync(IServiceProvider services, CancellationToken token)
		{
			//TODO: Here's a plan to realize
			//1. log in to youtube with OAuth or other credentials and userID (taken from configuration?)
			//2. check if on specific channel/list there are some videos
			//3. check if on the channel/list there is already video named XXXXX
			//4. if there is video XXXXX, exit sending some message and/or logging info
			//5. if there is no video XXXXX, prepare for video upload takine userID, credentials, perms etc.
			//6. upload video with retrys (3x ?) to channel/list and GET LINK TO it
			//7. send info with the link to some place (email?) and log something (link and XXXXX name?)

			throw new NotImplementedException("realize THE PLAN");
		}
	}
}
