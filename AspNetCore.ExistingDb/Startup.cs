using AspNetCore.ExistingDb.Repositories;
using AspNetCore.ExistingDb.Services;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using InkBall.Module;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

//[assembly: UserSecretsId("aspnet-AspNetCore.ExistingDb-20161230022416")]

namespace EFGetStarted.AspNetCore.ExistingDb
{
	public class Startup
	{
		public IConfiguration Configuration { get; }

		static async Task Main(string[] args)
		{
			var host = new WebHostBuilder()
				.UseKestrel()
				//.UseLibuv()
				.UseSockets()
				.UseLinuxTransport(async opts =>
				{
					await Console.Out.WriteLineAsync("Using Linux Transport");
				})
				.UseContentRoot(Directory.GetCurrentDirectory())
				//.UseIISIntegration()
				.UseStartup<Startup>()
				//.UseApplicationInsights()
				.Build();

			await host.RunAsync();

			//await CreateWebHostBuilder(args).Build().RunAsync();
		}

		//public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
		//	WebHost.CreateDefaultBuilder(args)
		//		.UseStartup<Startup>();

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
			BloggingContextFactory.ConfigureDBKind(null, configuration, services);
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

		void ConfigureDependencyInjection(IServiceCollection services)
		{
			services.AddSingleton(Configuration);
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
			//1st time init of static vars
			HashesRepository.HashesInfoExpirationInMinutes = TimeSpan.FromMinutes(Configuration.GetValue<int>(nameof(HashesRepository.HashesInfoExpirationInMinutes)));

			services.AddScoped<IThinHashesDocumentDBRepository, ThinHashesDocumentDBRepository>();
			//services.AddSingleton<IUrlHelperFactory, DomainUrlHelperFactory>();
			services.AddHostedService<BackgroundOperationService>();
			services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>(CreateBackgroundTaskQueue);
			services.AddServerTiming();

			services.AddTransient<MjpgStreamerHttpClientHandler>()
				.AddHttpClient<IMjpgStreamerHttpClient, MjpgStreamerHttpClient>()
				.ConfigurePrimaryHttpMessageHandler<MjpgStreamerHttpClientHandler>();
			services.AddCommonUI();
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			ConfigureDependencyInjection(services);

			services.AddDbContextPool<BloggingContext>(options =>
			{
				BloggingContextFactory.ConfigureDBKind(options, Configuration);
			});

			ConfigureDistributedCache(Configuration, services);

			services.AddSession(options =>
			{
				// Set a short timeout for easy testing.
				options.IdleTimeout = TimeSpan.FromMinutes(60);
				options.Cookie.HttpOnly = true;
			});

			// Add framework services.
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

			var keys_directory = Configuration["SharedKeysDirectory"]?.Replace('/', Path.DirectorySeparatorChar)?.Replace('\\', Path.DirectorySeparatorChar);
			if (!string.IsNullOrEmpty(keys_directory) && Directory.Exists(keys_directory))
			{
				services.AddDataProtection()
					.SetDefaultKeyLifetime(TimeSpan.FromDays(14))
					.PersistKeysToFileSystem(new DirectoryInfo(keys_directory));
			}
			else
			{
				services.AddDataProtection()
					.SetDefaultKeyLifetime(TimeSpan.FromDays(14));
			}

		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));

			// app.UsePathBase("/dotnet");
			app.Use((context, next) =>
			{
				context.Request.Scheme = "https";
				if (context.Request.Path.StartsWithSegments("/dotnet",
					out PathString matchedPath, out PathString remainingPath))
				{
					var originalPath = context.Request.Path;
					var originalPathBase = context.Request.PathBase;
					context.Request.Path = remainingPath;
					context.Request.PathBase = originalPathBase.Add(matchedPath);

					try
					{
						return next();
					}
					finally
					{
						context.Request.Path = originalPath;
						context.Request.PathBase = originalPathBase;
					}
				}
				else
					return next();
			});

			if (env.IsDevelopment())
			{
				loggerFactory.AddDebug();
				app.UseDeveloperExceptionPage();
				//app.UseExceptionHandler("/Home/Error");
				app.UseBrowserLink();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}
			app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

			app.UseStaticFiles();

			app.UseServerTiming();

			app.UseSession();

			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}
