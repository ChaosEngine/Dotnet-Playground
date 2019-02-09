﻿using AspNetCore.ExistingDb.Helpers;
using AspNetCore.ExistingDb.Repositories;
using AspNetCore.ExistingDb.Services;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using InkBall.Module;
using IdentitySample.DefaultUI.Data;
using IdentitySample.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

//[assembly: UserSecretsId("aspnet-AspNetCore.ExistingDb-20161230022416")]

namespace EFGetStarted.AspNetCore.ExistingDb
{
	public class Startup
	{
		public IConfiguration Configuration { get; }

		//public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
		//	WebHost.CreateDefaultBuilder(args).UseStartup<Startup>();

		static async Task Main(string[] args)
		{
			var host = new WebHostBuilder()
				.UseKestrel()
				//.UseLibuv()
				.UseSockets()
				/*.UseLinuxTransport(async opts =>
				{
					await Console.Out.WriteLineAsync("Using Linux Transport");
				})*/
				.UseContentRoot(Directory.GetCurrentDirectory())
				//.UseIISIntegration()
				.UseStartup<Startup>()
				//.UseApplicationInsights()
				.Build();

			await host.RunAsync();

			//await CreateWebHostBuilder(args).Build().RunAsync();
		}

		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
				.AddEnvironmentVariables();
			if (env.IsDevelopment())
				builder.AddUserSecrets<Startup>();

			Configuration = builder.Build();
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
					else if (!File.Exists("client_secrets.json"))
						return (int)YouTubeUploadOperation.ErrorCodes.CLIENT_SECRETS_NOT_EXISTING;
					else if (!File.Exists(found))
						return (int)YouTubeUploadOperation.ErrorCodes.VIDEO_FILE_NOT_EXISTING;
					else
					{
						//btq.QueueBackgroundWorkItem(new BeepBackgroundOperation(500, 250));
						btq.QueueBackgroundWorkItem(new YouTubeUploadOperation(found, "client_secrets.json"));
						return (int)YouTubeUploadOperation.ErrorCodes.OK;
					}
				},
				failRetryCount: 5)
			);

			return btq;
		}

		void ConfigureDependencyInjection(IServiceCollection services, IHostingEnvironment env)
		{
			services.AddSingleton(Configuration);

			services.AddLogging(loggingBuilder =>
			{
				loggingBuilder.AddConfiguration(Configuration.GetSection("Logging"));
				loggingBuilder.AddConsole();

				if (env.IsDevelopment())
					loggingBuilder.AddDebug();
			});
#if DEBUG
			//services.AddSingleton<ICompilationService, RoslynCompilationService>();
			services.AddSingleton<Microsoft.AspNetCore.Razor.Language.RazorTemplateEngine, CustomTemplateEngine>();
#endif
			services.AddScoped<IBloggingRepository, BloggingRepository>();

			string dbs_config;
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
			}
			else
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

			services.AddScoped<IThinHashesDocumentDBRepository, ThinHashesDocumentDBRepository>();
			services.AddSingleton<IUrlHelperFactory, DomainUrlHelperFactory>();
			services.AddHostedService<BackgroundOperationService>();
			services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>(CreateBackgroundTaskQueue);
			services.AddServerTiming();

			services.AddTransient<MjpgStreamerHttpClientHandler>()
				.AddHttpClient<IMjpgStreamerHttpClient, MjpgStreamerHttpClient>()
				.ConfigurePrimaryHttpMessageHandler<MjpgStreamerHttpClientHandler>();
		}

		private void ConfigureAuthenticationAuthorizationHelper(IServiceCollection services, IHostingEnvironment env)
		{
			services.AddTransient<IEmailSender, AuthMessageSender>();
			//services.AddTransient<ISmsSender, AuthMessageSender>();


			services.AddIdentity<ApplicationUser, IdentityRole>()
				.AddEntityFrameworkStores<BloggingContext>()
				.AddDefaultTokenProviders().AddSignInManager<MySignInManager>();

			var builder = services.AddAuthentication();
			if (!string.IsNullOrEmpty(Configuration["Authentication:Google:ClientId"]))
			{
				builder.AddOAuth<GoogleOptions, MyGoogleHandler>(
					GoogleDefaults.AuthenticationScheme, GoogleDefaults.DisplayName,
					googleOptions =>
					{
						googleOptions.ClientId = Configuration["Authentication:Google:ClientId"];
						googleOptions.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
						googleOptions.CallbackPath = Configuration["Authentication:Google:CallbackPath"];

						googleOptions.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";
						googleOptions.ClaimActions.Clear();
						googleOptions.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
						googleOptions.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
						googleOptions.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
						googleOptions.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
						googleOptions.ClaimActions.MapJsonKey("urn:google:profile", "link");
						googleOptions.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
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
				builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TwitterOptions>, TwitterPostConfigureOptions>());
				builder.AddRemoteScheme<TwitterOptions, MyTwitterHandler>(
					TwitterDefaults.AuthenticationScheme, TwitterDefaults.DisplayName,
					twitterOptions =>
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
				options.LoginPath = Configuration["AppRootPath"] + "Identity/Account/Login";
				options.AccessDeniedPath = Configuration["AppRootPath"] + "Identity/Account/AccessDenied";
			});

			services.AddAuthorization(options =>
			{
				options.AddPolicy("InkBallPlayerPolicy", policy =>
				{
					policy.RequireAuthenticatedUser()
						//.RequireRole("InkBallPlayer")
						.AddRequirements(new InkBall.Module.MinimumAgeRequirement(18));
				});
			});






			#region WIP

			services.AddInkBallCommonUI<InkBall.Module.Model.GamesContext, ApplicationUser>(env, options =>
			{
				// options.WwwRoot = "wrongwrongwrong";
				// options.HeadElementsSectionName = "head-head-head-Elements";
				// options.ScriptsSectionName = "Script_Injection";
				options.AuthorizationPolicyName = "InkBallPlayerPolicy";
				options.AppRootPath = Configuration["AppRootPath"];
				options.UseMessagePackBinaryTransport = true;
			});

			#endregion WIP



		}

		private void UseProxyForwardingAndDomainPathHelper(IApplicationBuilder app)
		{
#if DEBUG
			string path_to_replace = Configuration["AppRootPath"].TrimEnd('/');

			//Check for reverse proxing and bump HTTP scheme to https
			app.Use((context, next) =>
			{
				if (context.Request.Path.StartsWithSegments(path_to_replace, out var remainder))
					context.Request.Path = remainder;
				if (context.Request.Headers.ContainsKey(ForwardedHeadersDefaults.XForwardedHostHeaderName))
					context.Request.Scheme = "https";

				return next();
			});
#else
			//Apache/nginx proxy schould pass "X-Forwarded-Proto"
			app.UseForwardedHeaders(new ForwardedHeadersOptions
			{
				ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
			});
#endif
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			var env = services.FirstOrDefault(x => x.ServiceType == typeof(IHostingEnvironment)).ImplementationInstance as IHostingEnvironment;

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
				options.Cookie.HttpOnly = true;
			});

			// Add framework services.
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

			var protection_builder = services.AddDataProtection()
				.SetDefaultKeyLifetime(TimeSpan.FromDays(14))
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
				options.PayloadSerializerSettings.ContractResolver = new DefaultContractResolver();
			})
			.AddMessagePackProtocol(options =>
			{
				options.FormatterResolvers = new List<MessagePack.IFormatterResolver>()
				{
					MessagePack.Resolvers.StandardResolver.Instance
				};
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			UseProxyForwardingAndDomainPathHelper(app);

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				//app.UseExceptionHandler("/Home/Error");
				app.UseBrowserLink();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}
			app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

#if DEBUG
			if (env.IsDevelopment())
				app.UseHttpsRedirection();
#endif

			app.UseStaticFiles();

			app.UseServerTiming();

			app.UseSession();

			app.UseAuthentication();

			//app.LogRequestHeaders(loggerFactory, "Upgrade");//display all 'Upgrade:Connection' headers occurence
			app.UseSignalR(routes =>
			{
				routes.MapHub<InkBall.Module.Hubs.GameHub>('/' + InkBall.Module.Hubs.GameHub.HubName);
			});

			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}
