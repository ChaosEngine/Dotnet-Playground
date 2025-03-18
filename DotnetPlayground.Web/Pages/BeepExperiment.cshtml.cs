using DotnetPlayground.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetPlayground.Pages
{
	public sealed class BeepExperimentModel : PageModel
	{
		public const string ASPX = "BeepExperiment";

		private readonly IBackgroundTaskQueue _backgroundTaskQueue;

		public bool IsPlaying => MelodyBeepBackgroundOperation.IsPlaying;

		public bool Enabled =>
#if DEBUG
			true;
#else
			false;
#endif

		public BeepExperimentModel(IBackgroundTaskQueue backgroundTaskQueue)
		{
			_backgroundTaskQueue = backgroundTaskQueue;
		}

#if DEBUG
		public IActionResult OnPost()
		{
			_backgroundTaskQueue.QueueBackgroundWorkItem(new MelodyBeepBackgroundOperation());

			return base.RedirectToPage();
		}
#endif

	}
}
