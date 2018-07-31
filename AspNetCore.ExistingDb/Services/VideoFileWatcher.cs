using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Services
{
	/// <summary>
	/// Watches changes of specific files inside directory and executes action on file add/change/delete
	/// </summary>
	/// <seealso cref="AspNetCore.ExistingDb.Services.BackgroundOperationBase" />
	internal class VideoFileWatcherBackgroundTask : BackgroundOperationBase
	{
		private readonly string _filterGlobb, _imageDirectory;
		private readonly TimeSpan? _initialDelay;

		/// <summary>
		/// Initializes a new instance of the <see cref="VideoFileWatcherBackgroundTask"/> class.
		/// </summary>
		/// <param name="imageDirectory">The image directory.</param>
		/// <param name="filterGlobb">The filter globb.</param>
		/// <param name="initialDelay">The initial delay.</param>
		public VideoFileWatcherBackgroundTask(string imageDirectory, string filterGlobb, TimeSpan? initialDelay)
		{
			_imageDirectory = imageDirectory;
			_filterGlobb = filterGlobb;
			_initialDelay = initialDelay;
		}

		/// <summary>
		/// Does the work.
		/// </summary>
		/// <param name="services">The services.</param>
		/// <param name="cancellation">The cancellation.</param>
		/// <returns></returns>
		public async override Task DoWork(IServiceProvider services, CancellationToken cancellation)
		{
			if (_initialDelay.HasValue)
				await Task.Delay(_initialDelay.Value);

			using (var scope = services.CreateScope())
			{
				var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
				var logger = loggerFactory.CreateLogger<VideoFileWatcherBackgroundTask>();

				if (string.IsNullOrEmpty(_imageDirectory))
				{
					logger.LogWarning("'ImageDirectory' directory not found from configuration");
					return;
				}
				PhysicalFileProvider fileProvider = null;
				try
				{
					fileProvider = new PhysicalFileProvider(_imageDirectory);
					logger.LogInformation("Starting to watch for '{0}' inside '{1}'", _filterGlobb, _imageDirectory);

					int counter = 0;
					while (!cancellation.IsCancellationRequested)
					{
						IChangeToken change_token = fileProvider.Watch(_filterGlobb);
						if (change_token == null) break;

						var tcs = new TaskCompletionSource<int>();
						IDisposable callback = null;
						try
						{
							cancellation.Register((token) =>
							{
								tcs?.TrySetCanceled((CancellationToken)token);
							}, cancellation, false);

							callback = change_token.RegisterChangeCallback(state =>
							{
								counter++;
								logger.LogInformation("'{0}' changed {1} of times", _filterGlobb, counter);

								TaskCompletionSource<int> completion = (TaskCompletionSource<int>)state;
								completion.TrySetResult(counter);
							}, tcs);

							await tcs.Task.ConfigureAwait(false);
						}
						catch (TaskCanceledException ex)
						{
							logger.LogWarning(ex, "cancelled");
						}
						finally
						{
							callback?.Dispose(); callback = null;
						}

						await Task.Delay(500);
					}
				}
				catch (Exception)
				{
					throw;
				}
				finally
				{
					fileProvider?.Dispose();
				}
			}
		}
	}
}
