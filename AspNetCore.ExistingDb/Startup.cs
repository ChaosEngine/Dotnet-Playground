using AspNetCore.ExistingDb.Repositories;
using EFGetStarted.AspNetCore.ExistingDb.Models;
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
using System.Threading.Tasks;

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
				.UseSockets()
				.UseContentRoot(Directory.GetCurrentDirectory())
				//.UseIISIntegration()
				.UseStartup<Startup>()
				//.UseApplicationInsights()
				.Build();

			await host.RunAsync();
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
			BloggingContextFactory.ConfigureDBKind(null, configuration, services);
		}

		void ConfigureDependencyInjection(IServiceCollection services)
		{
			services.AddSingleton(Configuration);
#if DEBUG
			services.AddSingleton<ICompilationService, RoslynCompilationService>();
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
			
			services.AddScoped<IThinHashesDocumentDBRepository, ThinHashesDocumentDBRepository>();
			services.AddSingleton<IUrlHelperFactory, DomainUrlHelperFactory>();
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			ConfigureDependencyInjection(services);

			services.AddDbContext<BloggingContext>(options =>
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
			services.AddMvc();

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

			services.AddServerTiming();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));

			//app.UseEnvironmentTitleDisplay();

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
