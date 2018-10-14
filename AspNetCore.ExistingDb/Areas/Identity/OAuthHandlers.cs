﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
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
}