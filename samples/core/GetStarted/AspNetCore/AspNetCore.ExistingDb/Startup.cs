using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using AspNetCore.ExistingDb;

//[assembly: UserSecretsId("aspnet-AspNetCore.ExistingDb-20161230022416")]

namespace EFGetStarted.AspNetCore.ExistingDb
{
	public class Startup
	{
		public IConfiguration Configuration { get; }

		public static void Main(string[] args)
		{
			var host = new WebHostBuilder()
				.UseKestrel()
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseIISIntegration()
				.UseStartup<Startup>()
				.UseApplicationInsights()
				.Build();

			host.Run();
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

		private string GetDBConnString(IConfiguration configuration)
		{
			string conn_str;
			switch (configuration["DBKind"]?.ToLower())
			{
				case "mysql":
				case "mariadb":
				case "maria":
					conn_str = configuration.GetConnectionString("MySQL");
					break;
				case "sqlserver":
				case "mssql":
					conn_str = configuration.GetConnectionString("SqlServer");
					break;
				case "sqlite":
					conn_str = "Filename=./Blogging.db";
					break;
				default:
					throw new NotSupportedException($"Bad DBKind name");
			}
			return conn_str;
		}

		/// <summary>
		/// Sets up DB kind and connection
		/// </summary>
		/// <returns>connection string</returns>
		/// <param name="options"></param>
		/// <param name="configuration"></param>
		internal static string ConfigureDBKind(DbContextOptionsBuilder options, IConfiguration configuration)
		{
			string conn_str;
			switch (configuration["DBKind"]?.ToLower())
			{
				case "mysql":
				case "mariadb":
				case "maria":
					conn_str = configuration.GetConnectionString("MySQL");
					options.UseMySql(conn_str);
					break;
				case "sqlserver":
				case "mssql":
					conn_str = configuration.GetConnectionString("SqlServer");
					options.UseSqlServer(conn_str);
					break;
				case "sqlite":
					conn_str = "Filename=./Blogging.db";
					options.UseSqlite(conn_str);
					break;
				default:
					throw new NotSupportedException($"Bad DBKind name");
			}
			return conn_str;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton(Configuration);

			services.AddDbContext<BloggingContext>(options =>
			{
				ConfigureDBKind(options, Configuration);
			});

			services.AddDistributedSqlServerCache(options =>
			{
				var conn_str = GetDBConnString(Configuration);

				options.ConnectionString = conn_str;
				options.SchemaName = "dbo";
				options.TableName = nameof(SessionCache);
				options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);
			});

			services.AddSession(options =>
			{
				// Set a short timeout for easy testing.
				options.IdleTimeout = TimeSpan.FromMinutes(30);
				options.CookieHttpOnly = true;
			});

			// Add framework services.
			services.AddMvc();

			if (Directory.Exists(Path.DirectorySeparatorChar + "shared"))
			{
				services.AddDataProtection()
					//.DisableAutomaticKeyGeneration()
					.PersistKeysToFileSystem(new DirectoryInfo(Path.DirectorySeparatorChar + "shared"))
					.SetApplicationName("AspNetCore.ExistingDb" + Configuration.AppRootPath());
			}
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
