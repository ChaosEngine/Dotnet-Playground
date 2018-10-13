using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Helpers
{
	public class MyGoogleHandler : GoogleHandler
	{
		public MyGoogleHandler(IOptionsMonitor<GoogleOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
			: base(options, logger, encoder, clock)
		{ }

		public override Task<bool> ShouldHandleRequestAsync()
		{
			return Task.FromResult(Options.CallbackPath.Value.Replace("/dotnet", "") == Request.Path.Value);
		}
	}

	public class MyTwitterHandler : TwitterHandler
	{
		public MyTwitterHandler(IOptionsMonitor<TwitterOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
		   : base(options, logger, encoder, clock)
		{ }

		public override Task<bool> ShouldHandleRequestAsync()
		{
			return Task.FromResult(Options.CallbackPath.Value.Replace("/dotnet", "") == Request.Path.Value);
		}
	}

	public class MyGithubHandler : OAuthHandler<OAuthOptions>
	{
		public MyGithubHandler(IOptionsMonitor<OAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
		   : base(options, logger, encoder, clock)
		{ }

		public override Task<bool> ShouldHandleRequestAsync()
		{
			return Task.FromResult(Options.CallbackPath.Value.Replace("/dotnet", "") == Request.Path.Value);
		}
	}
}
