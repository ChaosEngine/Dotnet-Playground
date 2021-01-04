using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetPlayground.Services
{
	public interface IBaseBackgroundOperation
	{
		Task DoWorkAsync(IServiceProvider services, CancellationToken token);
	}

	public abstract class BackgroundOperationBase : IBaseBackgroundOperation
	{
		//protected delegate Task TaskCalculationFunction(IServiceProvider services, CancellationToken token);

		public abstract Task DoWorkAsync(IServiceProvider services, CancellationToken token);
	}

	public class DummyBackgroundOperation : BackgroundOperationBase
	{
		public DummyBackgroundOperation() { }

		public override Task DoWorkAsync(IServiceProvider services, CancellationToken token)
		{
			return Task.CompletedTask;
		}
	}

	public interface IBackgroundTaskQueue
	{
		void QueueBackgroundWorkItem(IBaseBackgroundOperation workItem);

		Task<IBaseBackgroundOperation> DequeueAsync(CancellationToken cancellationToken);
	}

	public sealed class BackgroundTaskQueue : IDisposable, IBackgroundTaskQueue
	{
		private readonly ConcurrentQueue<IBaseBackgroundOperation> _workItems = new ConcurrentQueue<IBaseBackgroundOperation>();

		private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

		/// <summary>
		/// Queues the background work item.
		/// </summary>
		/// <param name="workItem">The work item.</param>
		/// <exception cref="ArgumentNullException">workItem</exception>
		public void QueueBackgroundWorkItem(IBaseBackgroundOperation workItem)
		{
			if (workItem == null)
			{
				throw new ArgumentNullException(nameof(workItem));
			}

			_workItems.Enqueue(workItem);
			_signal.Release();
		}

		/// <summary>
		/// Dequeues the asynchronous. Blocks if nothing added to the queue.
		/// Waits indefinetelly or if cancel requested
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns></returns>
		public async Task<IBaseBackgroundOperation> DequeueAsync(CancellationToken cancellationToken)
		{
			await _signal.WaitAsync(cancellationToken);
			_workItems.TryDequeue(out var workItem);

			return workItem;
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
					_signal.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~BackgroundTaskQueue() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
