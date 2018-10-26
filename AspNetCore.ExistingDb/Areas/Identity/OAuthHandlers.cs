using IdentitySample.DefaultUI.Data;
using InkBall.Module;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
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

	public class MyGithubHandler : OAuthHandler<MyGithubHandler.GitHubOptions>
	{
		public class GitHubOptions : OAuthOptions
		{
			public GitHubOptions()
			{
				AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
				TokenEndpoint = "https://github.com/login/oauth/access_token";
				UserInformationEndpoint = "https://api.github.com/user";
				ClaimsIssuer = "OAuth2-Github";
				SaveTokens = true;
				// Retrieving user information is unique to each provider.
				ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
				ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
				ClaimActions.MapJsonKey("urn:github:name", "name");
				ClaimActions.MapJsonKey(ClaimTypes.Email, "email", ClaimValueTypes.Email);
				ClaimActions.MapJsonKey("urn:github:url", "url");
			}
		}

		public MyGithubHandler(IOptionsMonitor<GitHubOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
		   : base(options, logger, encoder, clock)
		{ }

		public override Task<bool> ShouldHandleRequestAsync()
		{
			return Task.FromResult(Options.CallbackPath.Value.Replace("/dotnet", "") == Request.Path.Value);
		}

		protected async override Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
		{
			using (var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint))
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				using (var response = await Backchannel.SendAsync(request, Context.RequestAborted))
				{
					if (!response.IsSuccessStatusCode)
					{
						throw new HttpRequestException($"An error occurred when retrieving Google user information ({response.StatusCode}). Please check if the authentication information is correct and the corresponding Google+ API is enabled.");
					}

					var payload = JObject.Parse(await response.Content.ReadAsStringAsync());

					var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, payload);
					context.RunClaimActions();

					await Events.CreatingTicket(context);
					return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
				}
			}
		}
	}

	public class MySignInManager : SignInManager<ApplicationUser>
	{
		private readonly GamesContext _inkBallContext;

		public MySignInManager(
			UserManager<ApplicationUser> userManager,
			IHttpContextAccessor contextAccessor,
			IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
			IOptions<IdentityOptions> optionsAccessor,
			ILogger<SignInManager<ApplicationUser>> logger,
			IAuthenticationSchemeProvider schemes,
			GamesContext inkBallContext
			) : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes)
		{
			_inkBallContext = inkBallContext;
		}

		public override async Task<ClaimsPrincipal> CreateUserPrincipalAsync(ApplicationUser user)
		{
			var principal = await base.CreateUserPrincipalAsync(user);
			
			// use this.UserManager if needed
			var identity = (ClaimsIdentity)principal.Identity;

			var name_identifer = principal.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!string.IsNullOrEmpty(name_identifer))
			{
				var external_id = name_identifer;
				InkBallUser found_user = _inkBallContext.InkBallUsers.FirstOrDefault(i => i.sExternalId == external_id);
				if (found_user != null)
				{
				}
				else
				{
					found_user = new InkBallUser
					{
						sExternalId = external_id,
						iPrivileges = 0,
					};
					await _inkBallContext.InkBallUsers.AddAsync(found_user, Context.RequestAborted);
					await _inkBallContext.SaveChangesAsync(true, Context.RequestAborted);
				}

				identity.AddClaim(new Claim("InkBallClaimType", found_user.iId.ToString(), "InkBallUser"));
			}

			if (!identity.HasClaim(x => x.Type == ClaimTypes.DateOfBirth))
			{
				if (user.Age > 0)
				{
					var date_of_birth = new Claim(ClaimTypes.DateOfBirth,
						DateTime.UtcNow.AddYears(-user.Age).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
					identity.AddClaim(date_of_birth);
				}
			}

			return principal;
		}
	}
}
