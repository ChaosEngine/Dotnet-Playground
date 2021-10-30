using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetPlayground.Services
{
	public sealed class BeepBackgroundOperation : BackgroundOperationBase
	{
		public BeepBackgroundOperation()
		{
		}

		public override Task DoWorkAsync(IServiceProvider services, CancellationToken token)
		{
			Console.Beep();

			return Task.CompletedTask;
		}
	}

	public sealed class MelodyBeepBackgroundOperation : BackgroundOperationBase
	{
		private static bool _isPlaying = false;
		private static CancellationTokenSource _cts;
		private static object _syncer = new object();

		public static bool IsPlaying
		{
			get
			{
				bool local;
				Monitor.Enter(_syncer);
				try
				{
					local = _isPlaying;
				}
				finally
				{
					Monitor.Exit(_syncer);
				}
				return local;
			}
		}

		public MelodyBeepBackgroundOperation()
		{
		}

		/// <summary>
		/// Taken from https://hashtagakash.wordpress.com/2014/01/22/182/
		/// </summary>
		private async Task Play(ILogger logger, CancellationToken token)
		{
			double speed_factor = 0.75;

			if (!OperatingSystem.IsWindows()) // standard guard examples
			{
				logger.LogInformation("Starting playing with console beep(s) and delays");

				Console.Beep();
				await Task.Delay((int)(375 * speed_factor), token);
				Console.Beep();
				await Task.Delay((int)(375 * speed_factor), token);
				Console.Beep();
				await Task.Delay((int)(375 * speed_factor), token);

				logger.LogInformation("Stopped playing");
			}
			else
			{
				logger.LogInformation("Starting playing Mario with console beep(s) and delays");

				Console.Beep(659, (int)(125 * speed_factor));
				Console.Beep(659, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(659, (int)(125 * speed_factor));
				await Task.Delay((int)(167 * speed_factor), token);
				Console.Beep(523, (int)(125 * speed_factor));
				Console.Beep(659, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(784, (int)(125 * speed_factor));
				await Task.Delay((int)(375 * speed_factor), token);
				Console.Beep(392, (int)(125 * speed_factor));
				await Task.Delay((int)(375 * speed_factor), token);
				Console.Beep(523, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(392, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(330, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(440, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(494, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(466, (int)(125 * speed_factor));
				await Task.Delay((int)(42 * speed_factor), token);
				Console.Beep(440, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(392, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(659, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(784, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(880, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(698, (int)(125 * speed_factor));
				Console.Beep(784, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(659, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(523, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(587, (int)(125 * speed_factor));
				Console.Beep(494, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(523, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(392, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(330, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(440, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(494, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(466, (int)(125 * speed_factor));
				await Task.Delay((int)(42 * speed_factor), token);
				Console.Beep(440, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(392, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(659, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(784, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(880, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(698, (int)(125 * speed_factor));
				Console.Beep(784, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(659, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(523, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(587, (int)(125 * speed_factor));
				Console.Beep(494, (int)(125 * speed_factor));
				await Task.Delay((int)(375 * speed_factor), token);
				Console.Beep(784, (int)(125 * speed_factor));
				Console.Beep(740, (int)(125 * speed_factor));
				Console.Beep(698, (int)(125 * speed_factor));
				await Task.Delay((int)(42 * speed_factor), token);
				Console.Beep(622, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(659, (int)(125 * speed_factor));
				await Task.Delay((int)(167 * speed_factor), token);
				Console.Beep(415, (int)(125 * speed_factor));
				Console.Beep(440, (int)(125 * speed_factor));
				Console.Beep(523, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(440, (int)(125 * speed_factor));
				Console.Beep(523, (int)(125 * speed_factor));
				Console.Beep(587, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(784, (int)(125 * speed_factor));
				Console.Beep(740, (int)(125 * speed_factor));
				Console.Beep(698, (int)(125 * speed_factor));
				await Task.Delay((int)(42 * speed_factor), token);
				Console.Beep(622, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(659, (int)(125 * speed_factor));
				await Task.Delay((int)(167 * speed_factor), token);
				Console.Beep(698, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(698, (int)(125 * speed_factor));
				Console.Beep(698, (int)(125 * speed_factor));
				await Task.Delay((int)(625 * speed_factor), token);
				Console.Beep(784, (int)(125 * speed_factor));
				Console.Beep(740, (int)(125 * speed_factor));
				Console.Beep(698, (int)(125 * speed_factor));
				await Task.Delay((int)(42 * speed_factor), token);
				Console.Beep(622, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(659, (int)(125 * speed_factor));
				await Task.Delay((int)(167 * speed_factor), token);
				Console.Beep(415, (int)(125 * speed_factor));
				Console.Beep(440, (int)(125 * speed_factor));
				Console.Beep(523, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(440, (int)(125 * speed_factor));
				Console.Beep(523, (int)(125 * speed_factor));
				Console.Beep(587, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(622, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(587, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(523, (int)(125 * speed_factor));
				await Task.Delay((int)(1125 * speed_factor), token);
				Console.Beep(784, (int)(125 * speed_factor));
				Console.Beep(740, (int)(125 * speed_factor));
				Console.Beep(698, (int)(125 * speed_factor));
				await Task.Delay((int)(42 * speed_factor), token);
				Console.Beep(622, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(659, (int)(125 * speed_factor));
				await Task.Delay((int)(167 * speed_factor), token);
				Console.Beep(415, (int)(125 * speed_factor));
				Console.Beep(440, (int)(125 * speed_factor));
				Console.Beep(523, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(440, (int)(125 * speed_factor));
				Console.Beep(523, (int)(125 * speed_factor));
				Console.Beep(587, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(784, (int)(125 * speed_factor));
				Console.Beep(740, (int)(125 * speed_factor));
				Console.Beep(698, (int)(125 * speed_factor));
				await Task.Delay((int)(42 * speed_factor), token);
				Console.Beep(622, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(659, (int)(125 * speed_factor));
				await Task.Delay((int)(167 * speed_factor), token);
				Console.Beep(698, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(698, (int)(125 * speed_factor));
				Console.Beep(698, (int)(125 * speed_factor));
				await Task.Delay((int)(625 * speed_factor), token);
				Console.Beep(784, (int)(125 * speed_factor));
				Console.Beep(740, (int)(125 * speed_factor));
				Console.Beep(698, (int)(125 * speed_factor));
				await Task.Delay((int)(42 * speed_factor), token);
				Console.Beep(622, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(659, (int)(125 * speed_factor));
				await Task.Delay((int)(167 * speed_factor), token);
				Console.Beep(415, (int)(125 * speed_factor));
				Console.Beep(440, (int)(125 * speed_factor));
				Console.Beep(523, (int)(125 * speed_factor));
				await Task.Delay((int)(125 * speed_factor), token);
				Console.Beep(440, (int)(125 * speed_factor));
				Console.Beep(523, (int)(125 * speed_factor));
				Console.Beep(587, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(622, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(587, (int)(125 * speed_factor));
				await Task.Delay((int)(250 * speed_factor), token);
				Console.Beep(523, (int)(125 * speed_factor));

				logger.LogInformation("Stopped playing Mario");
			}
		}

		public override async Task DoWorkAsync(IServiceProvider services, CancellationToken token)
		{
			Monitor.Enter(_syncer);
			try
			{
				if (_isPlaying)
				{
					//stop
					_cts.Cancel(true);
					return;
				}
			}
			finally
			{
				Monitor.Exit(_syncer);
			}

			if (!_isPlaying)
			{
				if (_cts != null)
				{
					_cts.Dispose();
					_cts = null;
				}
				_cts = CancellationTokenSource.CreateLinkedTokenSource(token);
				try
				{
					_isPlaying = true;
					using (var scope = services.CreateScope())
					{
						var logger = scope.ServiceProvider.GetRequiredService<ILogger<MelodyBeepBackgroundOperation>>();

						await Play(logger, _cts.Token);
					}
					_isPlaying = false;
				}
				catch (TaskCanceledException)
				{
					_isPlaying = false;
				}
			}

			// return Task.CompletedTask;
		}
	}
}
