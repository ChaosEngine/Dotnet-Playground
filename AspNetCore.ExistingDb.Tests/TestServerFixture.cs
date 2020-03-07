using AspNetCore.ExistingDb.Repositories;
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

		/*public TestServerFixture() : this(//"AspNetCore.ExistingDb"
			null)
		{
		}

		protected TestServerFixture(string relativeTargetProjectParentDir)
		{
			var startupAssembly = typeof(TStartup).GetTypeInfo().Assembly;
			var contentRoot = GetProjectPath(relativeTargetProjectParentDir, startupAssembly);

			Directory.SetCurrentDirectory(contentRoot);

			var builder = new WebHostBuilder()
				.UseContentRoot(contentRoot)
				.ConfigureServices(InitializeServices)
				.UseEnvironment("Development")
				.UseStartup(typeof(TStartup))
				//.UseApplicationInsights()
				;

			_server = new TestServer(builder);

			Client = _server.CreateClient();
			Client.BaseAddress = new Uri("http://localhost");

			var configuration = _server.Host.Services.GetService(typeof(IConfiguration)) as IConfiguration;
			AppRootPath = configuration?["AppRootPath"];
			DBKind = configuration?["DBKind"];
			ImageDirectory = configuration?["ImageDirectory"];
			LiveWebCamURL = configuration?["LiveWebCamURL"];

			string temp = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
			DOTNET_RUNNING_IN_CONTAINER = !string.IsNullOrEmpty(temp) && temp.Equals(true.ToString(), StringComparison.InvariantCultureIgnoreCase);
			//Console.WriteLine($"### temp = {temp}, DOTNET_RUNNING_IN_CONTAINER = {DOTNET_RUNNING_IN_CONTAINER}");

			var db = _server.Host.Services.GetRequiredService<EFGetStarted.AspNetCore.ExistingDb.Models.BloggingContext>();
			if (DBKind.Equals("sqlite",StringComparison.InvariantCultureIgnoreCase))
				db.Database.Migrate();
			else
				db.Database.EnsureCreated();
		}*/

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
		private static string GetProjectPath(string projectRelativePath, Assembly startupAssembly)
		{
			// Get name of the target project which we want to test
			var projectName = startupAssembly.GetName().Name;

			projectRelativePath = projectRelativePath ?? projectName;

			// Get currently executing test project path
			var applicationBasePath = AppContext.BaseDirectory;

			// Find the path to the target project
			var directoryInfo = new DirectoryInfo(applicationBasePath);
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
			while (directoryInfo.Parent != null);

			throw new Exception($"Project root could not be located using the application root {applicationBasePath}.");
		}

		/*#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
					Client.Dispose();
					_server.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~TestFixture() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion IDisposable Support*/


		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			string relativeTargetProjectParentDir = null;
			var startupAssembly = typeof(TStartup).GetTypeInfo().Assembly;
			var contentRoot = GetProjectPath(relativeTargetProjectParentDir, startupAssembly);

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

					var db = scopedServices.GetRequiredService<EFGetStarted.AspNetCore.ExistingDb.Models.BloggingContext>();
					if (DBKind.Equals("sqlite", StringComparison.InvariantCultureIgnoreCase))
						db.Database.Migrate();
					else
						db.Database.EnsureCreated();
				}
			});
		}
	}

	[CollectionDefinition(nameof(TestServerCollection))]
	public class TestServerCollection : ICollectionFixture<TestServerFixture<EFGetStarted.AspNetCore.ExistingDb.Startup>>
	{
		// This class has no code, and is never created. Its purpose is simply
		// to be the place to apply [CollectionDefinition] and all the
		// ICollectionFixture<> interfaces.
	}
}
