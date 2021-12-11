using DotnetPlayground.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Xunit;

namespace Integration
{
	/// <summary>
	/// A test fixture which hosts the target project (project we wish to test) in an in-memory server.
	/// </summary>
	/// <typeparam name="TStartup"/>Target project's startup type</typeparam>
	public class TestServerFixture<TStartup> : WebApplicationFactory<TStartup>
		where TStartup : class
	{
		//private readonly TestServer _server;

		public HttpClient Client
		{
			get
			{
				var client = this.CreateClient(new WebApplicationFactoryClientOptions
				{
					AllowAutoRedirect = false
				});
				//cl.BaseAddress = new Uri("http://localhost");
				return client;
			}
		}

		internal string AppRootPath { get; private set; }

		internal string DBKind { get; private set; }

		internal string ImageDirectory { get; private set; }

		internal string LiveWebCamURL { get; private set; }

		internal bool DOTNET_RUNNING_IN_CONTAINER { get; private set; }

		protected virtual void InitializeServices(IServiceCollection services)
		{
			var startupAssembly = typeof(TStartup).GetTypeInfo().Assembly;

			// Inject a custom application part manager. 
			// Overrides AddMvcCore() because it uses TryAdd().
			var manager = new ApplicationPartManager();
			manager.ApplicationParts.Add(new AssemblyPart(startupAssembly));
			manager.FeatureProviders.Add(new ControllerFeatureProvider());
			manager.FeatureProviders.Add(new ViewComponentFeatureProvider());

			services.AddSingleton(manager);
		}

		/// <summary>
		/// Gets the full path to the target project that we wish to test
		/// </summary>
		/// <param name="projectRelativePath">
		/// The parent directory of the target project.
		/// e.g. src, samples, test, or test/Websites
		/// </param>
		/// <param name="startupAssembly">The target project's assembly.</param>
		/// <returns>The full path to the target project.</returns>
		internal static string GetProjectPath(string projectRelativePath = null)
		{
			// Get name of the target project which we want to test
			var projectName = typeof(TStartup).GetTypeInfo().Assembly.GetName().Name;

			projectRelativePath = projectRelativePath ?? projectName;

			// Get currently executing test project path
			var applicationBasePath = AppContext.BaseDirectory;

			// Find the path to the target project
			var directoryInfo = new DirectoryInfo(applicationBasePath);
			int max_dir_deep_cnter = 10;
			do
			{
				directoryInfo = directoryInfo.Parent;

				var projectDirectoryInfo = directoryInfo.EnumerateDirectories().FirstOrDefault(d => d.Name == projectRelativePath);
				if (projectDirectoryInfo != null)
				{
					var projectFileInfo = new FileInfo(Path.Combine(projectDirectoryInfo.FullName, $"{projectName}.csproj"));
					if (projectFileInfo.Exists)
					{
						return projectDirectoryInfo.FullName;
					}
				}
			}
			while (directoryInfo.Parent != null && max_dir_deep_cnter-- > 0);

			throw new Exception($"Project root could not be located using the application root {applicationBasePath}.");
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			var contentRoot = GetProjectPath();

			Directory.SetCurrentDirectory(contentRoot);

			builder.UseContentRoot(contentRoot)
				//.ConfigureServices(InitializeServices)
				.UseEnvironment("Development")
				//.UseStartup(typeof(TStartup))
				//.UseApplicationInsights()
				;

			builder.ConfigureServices(services =>
			{
				// Build the service provider.
				var sp = services.BuildServiceProvider();

				// Create a scope to obtain a reference to the database context (ApplicationDbContext).
				using (var scope = sp.CreateScope())
				{
					var scopedServices = scope.ServiceProvider;
					//var db = scopedServices.GetRequiredService<ApplicationDbContext>();
					//var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

					//// Ensure the database is created.
					//db.Database.EnsureCreated();

					//try
					//{
					//	// Seed the database with test data.
					//	Utilities.InitializeDbForTests(db);
					//}
					//catch (Exception ex)
					//{
					//	logger.LogError(ex, "An error occurred seeding the " +
					//		"database with test messages. Error: {Message}", ex.Message);
					//}

					var configuration = scopedServices.GetService(typeof(IConfiguration)) as IConfiguration;
					AppRootPath = configuration?["AppRootPath"];
					DBKind = configuration?["DBKind"];
					ImageDirectory = configuration?["ImageDirectory"];
					LiveWebCamURL = configuration?["LiveWebCamURL"];

					string temp = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
					DOTNET_RUNNING_IN_CONTAINER = !string.IsNullOrEmpty(temp) && temp.Equals(true.ToString(), StringComparison.InvariantCultureIgnoreCase);
					//Console.WriteLine($"### temp = {temp}, DOTNET_RUNNING_IN_CONTAINER = {DOTNET_RUNNING_IN_CONTAINER}");

					var db = scopedServices.GetRequiredService<DotnetPlayground.Models.BloggingContext>();
					if (DBKind.Equals("sqlite", StringComparison.InvariantCultureIgnoreCase))
						db.Database.Migrate();
					else
						db.Database.EnsureCreated();
				}
			});
		}
	}

	[CollectionDefinition(nameof(TestServerCollection))]
	public class TestServerCollection : ICollectionFixture<TestServerFixture<DotnetPlayground.Startup>>
	{
		// This class has no code, and is never created. Its purpose is simply
		// to be the place to apply [CollectionDefinition] and all the
		// ICollectionFixture<> interfaces.
	}
}
