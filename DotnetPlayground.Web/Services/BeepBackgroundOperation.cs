using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetPlayground.Services
{
	public class BeepBackgroundOperation : BackgroundOperationBase
	{
		private readonly int _frequency, _duration;

		public BeepBackgroundOperation(int frequency, int duration)
		{
			_frequency = frequency;
			_duration = duration;
		}

		public override Task DoWorkAsync(IServiceProvider services, CancellationToken token)
		{
#pragma warning disable CA1416 // Validate platform compatibility
			Console.Beep(_frequency, _duration);
#pragma warning restore CA1416 // Validate platform compatibility

			return Task.CompletedTask;
		}
	}
}
