﻿using DotnetPlayground.Models;
using InkBall.Module;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace DotnetPlayground.Helpers
{
	public class MyGoogleHandler : GoogleHandler
	{
		//private readonly IConfiguration _configuration;

		public MyGoogleHandler(IOptionsMonitor<GoogleOptions> options, ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration)
			: base(options, logger, encoder)
		{
			//_configuration = configuration;
		}

		// public override Task<bool> ShouldHandleRequestAsync()
		// {
		// 	return Task.FromResult(Options.CallbackPath.Value.Replace(_configuration["AppRootPath"], "/") == Request.Path.Value);
		// }
	}

	public class MyTwitterHandler : TwitterHandler
	{
		//private readonly IConfiguration _configuration;

		public MyTwitterHandler(IOptionsMonitor<TwitterOptions> options, ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration)
		   : base(options, logger, encoder)
		{
			//_configuration = configuration;
		}

		// public override Task<bool> ShouldHandleRequestAsync()
		// {
		// 	return Task.FromResult(Options.CallbackPath.Value.Replace(_configuration["AppRootPath"], "/") == Request.Path.Value);
		// }
	}

	public class MyGithubHandler : OAuthHandler<MyGithubHandler.GitHubOptions>
	{
		//private readonly IConfiguration _configuration;

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

		public MyGithubHandler(IOptionsMonitor<GitHubOptions> options, ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration)
		   : base(options, logger, encoder)
		{
			//_configuration = configuration;
		}

		// public override Task<bool> ShouldHandleRequestAsync()
		// {
		// 	return Task.FromResult(Options.CallbackPath.Value.Replace(_configuration["AppRootPath"], "/") == Request.Path.Value);
		// }

		protected async override Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
		{
			var token = Context.RequestAborted;
			using (var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint))
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(System.Net.Mime.MediaTypeNames.Application.Json));

				using (var response = await Backchannel.SendAsync(request, token))
				{
					if (!response.IsSuccessStatusCode)
					{
						throw new HttpRequestException($"An error occurred when retrieving user information ({response.StatusCode}). Please check if the authentication information is correct and the corresponding API is enabled.");
					}

					var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token)).RootElement;
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
		public MySignInManager(
			UserManager<ApplicationUser> userManager,
			IHttpContextAccessor contextAccessor,
			IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
			IOptions<IdentityOptions> optionsAccessor,
			ILogger<SignInManager<ApplicationUser>> logger,
			IAuthenticationSchemeProvider schemes,
			IUserConfirmation<ApplicationUser> confirmation
			) : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
		{
		}

		public override async Task<ClaimsPrincipal> CreateUserPrincipalAsync(ApplicationUser user)
		{
			var principal = await base.CreateUserPrincipalAsync(user);

			await Authentication.InkBallCreateUserPrincipalAsync(Context, user, principal);

			return principal;
		}

		public override async Task SignOutAsync()
		{
			//TODO: it is questionable whether to execute player-loosing when intentionally signing out
			//await Authentication.InkBallSignOutActionAsync(Context, Logger, Context.User.FindFirstValue(ClaimTypes.NameIdentifier));

			await base.SignOutAsync();
		}
	}
}
