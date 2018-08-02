using System;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Services
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
			Console.Beep(_frequency, _duration);

			return Task.CompletedTask;
		}
	}
}
