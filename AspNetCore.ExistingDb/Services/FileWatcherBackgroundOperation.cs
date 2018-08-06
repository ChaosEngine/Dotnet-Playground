using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Services
{
	/// <summary>
	/// Watches changes of specific files inside directory and executes action on file add/change/delete
	/// </summary>
	/// <seealso cref="AspNetCore.ExistingDb.Services.BackgroundOperationBase" />
	internal class FileWatcherBackgroundOperation : BackgroundOperationBase
	{
		private readonly string _filterGlobb, _directoryToWatch;
		private readonly TimeSpan? _initialDelay;
		private readonly Func<int, string, string, bool> _onChangeFunction;
		private readonly int? _failRetryCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="FileWatcherBackgroundOperation" /> class.
		/// </summary>
		/// <param name="directoryToWatch">The directory to watch.</param>
		/// <param name="filterGlobing">The filter globing.</param>
		/// <param name="initialDelay">The initial delay.</param>
		/// <param name="onChangeFunction">The on change function.</param>
		/// <param name="failRetryCount">The fail retry count.</param>
		public FileWatcherBackgroundOperation(string directoryToWatch, string filterGlobing, TimeSpan? initialDelay,
			Func<int, string, string, bool> onChangeFunction, int? failRetryCount = null)
		{
			_directoryToWatch = directoryToWatch;
			_filterGlobb = filterGlobing;
			_initialDelay = initialDelay;
			_onChangeFunction = onChangeFunction;
			_failRetryCount = failRetryCount;
		}

		/// <summary>
		/// Does the work.
		/// </summary>
		/// <param name="services">The services.</param>
		/// <param name="cancellation">The cancellation.</param>
		/// <returns></returns>
		public async override Task DoWorkAsync(IServiceProvider services, CancellationToken cancellation)
		{
			if (_initialDelay.HasValue)
				await Task.Delay(_initialDelay.Value);

			using (var scope = services.CreateScope())
			{
				var logger = scope.ServiceProvider.GetRequiredService<ILogger<FileWatcherBackgroundOperation>>();

				if (string.IsNullOrEmpty(_directoryToWatch))
				{
					logger.LogWarning("'ImageDirectory' directory not found from configuration");
					return;
				}
				PhysicalFileProvider fileProvider = null;
				try
				{
					fileProvider = new PhysicalFileProvider(_directoryToWatch);
					logger.LogInformation("Starting to watch for '{0}' inside '{1}'", _filterGlobb, _directoryToWatch);

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
								bool? result;
								int fail_count = _failRetryCount ?? 1;
								do
								{
									result = _onChangeFunction?.Invoke(counter, _directoryToWatch, _filterGlobb);
									logger.LogInformation("'{0}' changed {1} of times, scheduled action with {state}", _filterGlobb,
										counter, result.GetValueOrDefault(false) ? "success" : "fail");
								}
								while (--fail_count > 0 && !result.GetValueOrDefault(false));

								((TaskCompletionSource<int>)state).TrySetResult(counter);
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
