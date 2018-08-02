using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Services
{
	public interface IBackgroundOperationService
	{
		Task StartAsync(CancellationToken cancellationToken);

		Task StopAsync(CancellationToken cancellationToken);
	}

	public sealed class BackgroundOperationService : IHostedService, IBackgroundOperationService
	{
		private readonly ILogger<BackgroundOperationService> _logger;
		private readonly IServiceProvider _services;
		private readonly IBackgroundTaskQueue _queue;
		private CancellationTokenSource _shutdown;
		private Task _worker;

		public BackgroundOperationService(IBackgroundTaskQueue queue, ILogger<BackgroundOperationService> logger, IServiceProvider services)
		{
			_logger = logger;
			_queue = queue;
			_services = services;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"### {nameof(BackgroundOperationService)} starting");

			_shutdown = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			_worker = Task.Run(DoWork, cancellationToken);

			return Task.CompletedTask;
		}

		private async Task DoWork()
		{
			while (!_shutdown.IsCancellationRequested)
			{
				var work_item = await _queue.DequeueAsync(_shutdown.Token);

				var backgroundTask = Task.Run(() =>
				{
					work_item.DoWorkAsync(_services, _shutdown.Token).ConfigureAwait(false);
				}, _shutdown.Token).ConfigureAwait(false);

				await Task.Delay(TimeSpan.FromMilliseconds(500), _shutdown.Token);
			}

			_logger.LogInformation($"### worker ending");
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"### {nameof(BackgroundOperationService)} stopping");

			// Stop called without start
			if (_worker == null)
				return Task.CompletedTask;

			_shutdown.Cancel();

			return Task.WhenAny(_worker, Task.Delay(Timeout.Infinite, cancellationToken));
		}
	}
}
