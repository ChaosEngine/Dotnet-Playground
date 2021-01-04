#if DEBUG
using Abiosoft.DotNet.DevReload;
#endif
using DotnetPlayground.Helpers;
using DotnetPlayground.Repositories;
using DotnetPlayground.Services;
using DotnetPlayground.Models;
using IdentityManager2.AspNetIdentity;
using IdentityManager2.Configuration;
using InkBall.Module;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using IdentityManager2.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MessagePack;

//[assembly: UserSecretsId("aspnet-DotnetPlayground-20161230022416")]

namespace DotnetPlayground
{
	public class Startup
	{
		public IConfiguration Configuration { get; }

		#region Main

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
			.ConfigureWebHostDefaults(webBuilder =>
			{
				webBuilder
				.UseKestrel(opts =>
				{
					opts.AddServerHeader = false;
					string ASPNETCORE_URLS = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
					if (!string.IsNullOrEmpty(ASPNETCORE_URLS) && ASPNETCORE_URLS.Contains("unix:/"))
					{
						opts.ListenUnixSocket("/sockets/www.sock");
					}
				})
				//.UseLibuv()
				.UseSockets()
				/*.UseLinuxTransport(async opts =>
				{
					await Console.Out.WriteLineAsync("Using Linux Transport");
				})*/
				.UseContentRoot(Directory.GetCurrentDirectory())
				//.UseIISIntegration()
				.UseStartup<Startup>();
			});

		static async Task Main(string[] args)
		{
			//// Adding following lines in order to mitigate:
			//// https://github.com/aspnet/Announcements/issues/405
			//// https://github.com/neuecc/MessagePack-CSharp/security/advisories/GHSA-7q36-4xx7-xcxf
			//// Enable additional security in MessagePack to handle untrusted data.
			//MessagePackSecurity.Active = MessagePackSecurity.UntrustedData;


			//await CreateHostBuilder(args).Build().RunAsync();
			var host = new HostBuilder()
				.UseContentRoot(Directory.GetCurrentDirectory())
				.ConfigureAppConfiguration((hostingContext, config) =>
				{
					config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
						.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
						.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true)
						.AddEnvironmentVariables();
					if (hostingContext.HostingEnvironment.IsDevelopment())
						config.AddUserSecrets<Startup>();
				})
				.ConfigureWebHost(webBuilder =>
				{
					webBuilder
					.UseKestrel(opts =>
					{
						opts.AddServerHeader = false;
						string ASPNETCORE_URLS = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
						if (!string.IsNullOrEmpty(ASPNETCORE_URLS) && ASPNETCORE_URLS.Contains("unix:/"))
						{
							opts.ListenUnixSocket("/sockets/www.sock");
						}
					})
					//.UseLibuv()
					.UseSockets()
					/*.UseLinuxTransport(async opts =>
					{
						await Console.Out.WriteLineAsync("Using Linux Transport");
					})*/
					//.UseIISIntegration()
					.UseStartup<Startup>();
				})
				.Build();

			await host.RunAsync();
		}

		#endregion Main

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		void ConfigureDistributedCache(IConfiguration configuration, IServiceCollection services)
		{
			ContextFactory.ConfigureDBKind(null, configuration, services);
		}

		private BackgroundTaskQueue CreateBackgroundTaskQueue(IServiceProvider serv)
		{
			var btq = new BackgroundTaskQueue();

			//Initially add and start file watching task for watching video file change
			//inside image directory
			btq.QueueBackgroundWorkItem(new FileWatcherBackgroundOperation(
				directoryToWatch: Configuration["ImageDirectory"],
				filterGlobing: "*.webm",
				initialDelay: TimeSpan.FromSeconds(3),
				onChangeFunction: (counter, dirToWatch, filter) =>
				{
					string found = Directory.EnumerateFiles(dirToWatch, filter, SearchOption.TopDirectoryOnly).FirstOrDefault();
					if (found == null)
						return (int)YouTubeUploadOperation.ErrorCodes.NO_VIDEO_FILE;
					else if (string.IsNullOrEmpty(Configuration["YouTubeAPI:ClientSecretsFileName"]) ||
						!File.Exists(Configuration["YouTubeAPI:ClientSecretsFileName"]))
					{
						return (int)YouTubeUploadOperation.ErrorCodes.CLIENT_SECRETS_NOT_EXISTING;
					}
					else if (!File.Exists(found))
						return (int)YouTubeUploadOperation.ErrorCodes.VIDEO_FILE_NOT_EXISTING;
					else
					{
						//btq.QueueBackgroundWorkItem(new BeepBackgroundOperation(500, 250));
						btq.QueueBackgroundWorkItem(new YouTubeUploadOperation(found));
						return (int)YouTubeUploadOperation.ErrorCodes.OK;
					}
				},
				failRetryCount: 5)
			);

			return btq;
		}

		void ConfigureDependencyInjection(IServiceCollection services, IWebHostEnvironment env)
		{
			services.AddLogging(loggingBuilder =>
			{
				loggingBuilder.AddConfiguration(Configuration.GetSection("Logging"));
				loggingBuilder.AddConsole();

				if (env.IsDevelopment())
					loggingBuilder.AddDebug();
			});
#if DEBUG
			//services.AddApplicationInsightsTelemetry();
#endif
			services.AddScoped<IBloggingRepository, BloggingRepository>();

			string dbs_config;
#if INCLUDE_COSMOSDB
			if (Configuration.GetSection("CosmosDB")?["enabled"] == true.ToString())
			{
				services.AddScoped<IHashesRepositoryPure, ThinHashesDocumentDBRepository>(serviceProvider =>
				{
					var conf = Configuration.GetSection("CosmosDB");
					string endpoint = conf["Endpoint"];
					string key = conf["Key"];
					string databaseId = conf["DatabaseId"];
					string collectionId = conf["CollectionId"];

					var db = new ThinHashesDocumentDBRepository(endpoint, key, databaseId, collectionId);
					return db;
				});
				dbs_config = Configuration["DBKind"]?.ToLower() + "+CosmosDB";

				services.AddScoped<IThinHashesDocumentDBRepository, ThinHashesDocumentDBRepository>();
			}
			else
#endif
			{
				services.AddScoped<IHashesRepositoryPure, HashesRepository>();
				dbs_config = Configuration["DBKind"]?.ToLower();
			}
			services.Configure<DBConfigShower>(options =>
			{
				options.DBConfig = dbs_config;
			});
			services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));

			//1st time init of static vars
			HashesRepository.HashesInfoExpirationInMinutes = TimeSpan.FromMinutes(Configuration.GetValue<int>(nameof(HashesRepository.HashesInfoExpirationInMinutes)));

			//services.AddSingleton<IUrlHelperFactory, DomainUrlHelperFactory>();
			services.AddHostedService<BackgroundOperationService>();
			services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>(CreateBackgroundTaskQueue);
			services.AddServerTiming();

			services.AddTransient<MjpgStreamerHttpClientHandler>()
				.AddHttpClient<IMjpgStreamerHttpClient, MjpgStreamerHttpClient>()
				.ConfigurePrimaryHttpMessageHandler<MjpgStreamerHttpClientHandler>();
		}

		private void ConfigureAuthenticationAuthorizationHelper(IServiceCollection services, IWebHostEnvironment env)
		{
			services.AddTransient<IEmailSender, AuthMessageSender>();
			//services.AddTransient<ISmsSender, AuthMessageSender>();


			services.AddIdentity<ApplicationUser, IdentityRole>()
				.AddEntityFrameworkStores<BloggingContext>()
				.AddDefaultTokenProviders().AddSignInManager<MySignInManager>();

			var builder = services.AddAuthentication();
			if (!string.IsNullOrEmpty(Configuration["Authentication:Google:ClientId"]))
			{
				builder.AddGoogle(googleOptions =>
				{
					googleOptions.ClientId = Configuration["Authentication:Google:ClientId"];
					googleOptions.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
					googleOptions.CallbackPath = Configuration["Authentication:Google:CallbackPath"];

					//googleOptions.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";
					//googleOptions.ClaimActions.Clear();
					//googleOptions.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
					//googleOptions.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
					//googleOptions.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
					//googleOptions.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
					//googleOptions.ClaimActions.MapJsonKey("urn:google:profile", "link");
					//googleOptions.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
				});
			}
			if (!string.IsNullOrEmpty(Configuration["Authentication:Facebook:AppId"]))
			{
				builder.AddFacebook(facebookOptions =>
				{
					facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
					facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
				});
			}
			if (!string.IsNullOrEmpty(Configuration["Authentication:Twitter:ConsumerKey"]))
			{
				//builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TwitterOptions>, TwitterPostConfigureOptions>());
				builder.AddTwitter(twitterOptions =>
				{
					twitterOptions.ConsumerKey = Configuration["Authentication:Twitter:ConsumerKey"];
					twitterOptions.ConsumerSecret = Configuration["Authentication:Twitter:ConsumerSecret"];
					twitterOptions.CallbackPath = Configuration["Authentication:Twitter:CallbackPath"];
				});
			}
			if (!string.IsNullOrEmpty(Configuration[$"Authentication:GitHub:{env.EnvironmentName}-ClientID"]))
			{
				builder.AddOAuth<MyGithubHandler.GitHubOptions, MyGithubHandler>("GitHub", "GitHub", gitHubOptions =>
				{
					gitHubOptions.ClientId = Configuration[$"Authentication:GitHub:{env.EnvironmentName}-ClientID"];
					gitHubOptions.ClientSecret = Configuration[$"Authentication:GitHub:{env.EnvironmentName}-ClientSecret"];
					gitHubOptions.CallbackPath = Configuration["Authentication:GitHub:CallbackPath"];
				});
			}

			services.ConfigureApplicationCookie(options =>
			{
				options.LoginPath = "/Identity/Account/Login";
				options.AccessDeniedPath = "/Identity/Account/AccessDenied";
				options.Cookie.HttpOnly = true;
				options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
				options.Cookie.SameSite = SameSiteMode.Strict;
			});



			services.AddAuthorization(options =>
			{
				options.AddPolicy("RequireAdministratorRole",
					policy => policy.RequireRole("Administrator"));
			})
			.AddInkBallCommonUI<InkBall.Module.Model.GamesContext, ApplicationUser>(env.WebRootFileProvider, options =>
			{
				// options.WwwRoot = "wrongwrongwrong";
				// options.HeadElementsSectionName = "head-head-head-Elements";
				// options.ScriptsSectionName = "Script_Injection";
				options.AppRootPath = Configuration["AppRootPath"];
				options.UseMessagePackBinaryTransport = true;
				// options.CustomAuthorizationPolicyBuilder = (policy) =>
				// {
				// 	policy.RequireAuthenticatedUser();
				// };
				options.LoginPath = Configuration["AppRootPath"] + "Identity/Account/Login";
				options.LogoutPath = Configuration["AppRootPath"] + "Identity/Account/Logout";
				options.RegisterPath = Configuration["AppRootPath"] + "Identity/Account/Register";
			});


			services.AddIdentityManager(options =>
			{
				options.SecurityConfiguration.RoleClaimType = "role";
				options.SecurityConfiguration.AdminRoleName = "IdentityManagerAdministrator";
				options.SecurityConfiguration.AuthenticationScheme = null;
				//options.SecurityConfiguration.ShowLoginButton = false;
				options.SecurityConfiguration.HostAuthenticationType = IdentityConstants.ApplicationScheme;
				//options.SecurityConfiguration.HostChallengeType = "Identity.Application";
				options.SecurityConfiguration.LoginPath = "/Identity/Account/Login";
				options.SecurityConfiguration.LogoutPath = "/Identity/Account/Logout";
				//options.SecurityConfiguration.PageRouteAttribute = "idm";
				//options.RootPathBase = Configuration["AppRootPath"].TrimEnd('/');
				options.TitleNavBarLinkTarget = Configuration["AppRootPath"];
			})
			.AddIdentityMangerService<AspNetCoreIdentityManagerService<ApplicationUser, string, IdentityRole, string>>();
		}

		private void UseProxyForwardingAndDomainPathHelper(IApplicationBuilder app)
		{
#if DEBUG
			/*string path_to_replace = Configuration["AppRootPath"].TrimEnd('/');

			//Check for reverse proxing and bump HTTP scheme to https
			app.Use((context, next) =>
			{
				if (context.Request.Path.StartsWithSegments(path_to_replace, out var remainder))
					context.Request.Path = remainder;
				if (context.Request.Headers.ContainsKey(ForwardedHeadersDefaults.XForwardedHostHeaderName))
					context.Request.Scheme = "https";

				return next();
			});*/
#else
			//Apache/nginx proxy schould pass "X-Forwarded-Proto"
			app.UseForwardedHeaders(new ForwardedHeadersOptions
			{
				ForwardedHeaders = /*ForwardedHeaders.XForwardedHost | */ForwardedHeaders.XForwardedProto
			});
#endif
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			var env = services.FirstOrDefault(x => x.ServiceType == typeof(IWebHostEnvironment)).ImplementationInstance as IWebHostEnvironment;

			ConfigureDependencyInjection(services, env);

			services.AddDbContextPool<BloggingContext>(options =>
			{
				ContextFactory.ConfigureDBKind(options, Configuration);
			});
			services.AddDbContextPool<InkBall.Module.Model.GamesContext>(options =>
			{
				ContextFactory.ConfigureDBKind(options, Configuration);
			});

			ConfigureAuthenticationAuthorizationHelper(services, env);

			ConfigureDistributedCache(Configuration, services);

			services.AddSession(options =>
			{
				// Set a short timeout for easy testing.
				options.IdleTimeout = TimeSpan.FromMinutes(60);
				options.Cookie.Path = Configuration["AppRootPath"].TrimEnd('/');
				options.Cookie.HttpOnly = true;
				options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
				options.Cookie.SameSite = SameSiteMode.Strict;
			});

			// Add framework services.
			services.AddControllersWithViews(options =>
			{
				options.UseCentralRoutePrefix<PageController>(new RouteAttribute("idm"));
			}).AddSessionStateTempDataProvider();
			services.AddRazorPages();

			var protection_builder = services.AddDataProtection()
				//.SetDefaultKeyLifetime(TimeSpan.FromDays(14))	//the default id 90 days - enough
				.PersistKeysToDbContext<BloggingContext>();
			if (!string.IsNullOrEmpty(Configuration["DataProtection:CertFile"]))
				protection_builder.ProtectKeysWithCertificate(new X509Certificate2(Configuration["DataProtection:CertFile"], Configuration["DataProtection:CertPassword"]));


			services.AddSignalR(options =>
			{
				options.EnableDetailedErrors = true;
				//options.SupportedProtocols = new System.Collections.Generic.List<string>(new[] { "websocket" });
#if DEBUG
				options.KeepAliveInterval = TimeSpan.FromSeconds(30);
				options.ClientTimeoutInterval = options.KeepAliveInterval * 2;
#endif
			})
			.AddJsonProtocol(options =>
			{
				//options.PayloadSerializerSettings.ContractResolver = new DefaultContractResolver();
			})
			.AddMessagePackProtocol(options =>
			{
				//options.SerializerOptions.WithResolver(MessagePack.Resolvers.StandardResolver.Instance);
				options.SerializerOptions = MessagePackSerializerOptions
					.Standard
					.WithResolver(MessagePack.Resolvers.StandardResolver.Instance)
					.WithSecurity(MessagePackSecurity.UntrustedData);
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			UseProxyForwardingAndDomainPathHelper(app);

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
#if DEBUG
				if (!System.Diagnostics.Debugger.IsAttached)
					app.UseDevReload(new MyDevReloadOptions(Configuration["AppRootPath"]));
#endif
				//app.UseExceptionHandler(Configuration["AppRootPath"] + "Home/Error");
				//app.UseBrowserLink();
			}
			else
			{
				app.UseExceptionHandler(Configuration["AppRootPath"] + "Home/Error");
			}
			app.UseStatusCodePagesWithReExecute(Configuration["AppRootPath"] + "Home/Error/{0}");
#if DEBUG
			if (env.IsDevelopment())
				app.UseHttpsRedirection();
#endif

			app.Map("/dotnet", main =>
			{
				main.UseStaticFiles();
				main.UseRouting();
				main.UseServerTiming();
				main.UseSession();
				main.UseAuthentication();
				main.UseAuthorization();

				main.UseEndpoints(endpoints =>
				{
					//endpoints.MapHub<InkBall.Module.Hubs.GameHub>("/" + InkBall.Module.Hubs.GameHub.HubName);
					endpoints.PrepareSignalRForInkBall("/");
#if DEBUG
					if (!System.Diagnostics.Debugger.IsAttached)
						endpoints.MapHub<DevReloadHub>("/DevReloadSignalR");
#endif
					endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
					endpoints.MapRazorPages();
				});

				main.UseIdentityManager();
			});
		}
	}
}
