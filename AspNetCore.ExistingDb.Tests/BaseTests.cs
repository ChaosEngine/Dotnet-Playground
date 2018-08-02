using EFGetStarted.AspNetCore.ExistingDb;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Tests
{
	public abstract class BaseRepositoryTests : IDisposable
	{
		public (SqliteConnection Conn, DbContextOptions<BloggingContext> DbOpts, IConfiguration Conf, IMemoryCache Cache, ILogger<Repositories.HashesRepository> Logger) Setup
		{
			get; set;
		}

		public CancellationToken CancellationToken
		{
			get
			{
				CancellationTokenSource source = new CancellationTokenSource();
				CancellationToken token = source.Token;
				return token;
			}
		}

		protected async Task<(SqliteConnection, DbContextOptions<BloggingContext>, IConfiguration, IMemoryCache, ILogger<Repositories.HashesRepository>)> SetupInMemoryDB()
		{
			var builder = new ConfigurationBuilder()
				.AddJsonFile("config.json", optional: false, reloadOnChange: true);
			var config = builder.Build();

			var connection = new SqliteConnection(config.GetConnectionString("Sqlite"));

			// In-memory database only exists while the connection is open
			await connection.OpenAsync();

			var options = new DbContextOptionsBuilder<BloggingContext>()
				.UseSqlite(connection)
				.Options;

			// Create the schema in the database
			using (var context = new BloggingContext(options))
			{
				await context.Database.EnsureCreatedAsync();
			}

			var serviceCollection = new ServiceCollection()
				.AddMemoryCache()
				.AddLogging();
			serviceCollection.AddDataProtection();
			var serviceProvider = serviceCollection.BuildServiceProvider();

			IMemoryCache cache = serviceProvider.GetService<IMemoryCache>();

			var logger = serviceProvider.GetService<ILoggerFactory>()
				.CreateLogger<Repositories.HashesRepository>();
			
			return (connection, options, config, cache, logger);
		}

		public BaseRepositoryTests()
		{
			var db = SetupInMemoryDB();
			db.Wait();
			Setup = db.Result;

			//// Define the cancellation token.
			//CancellationTokenSource source = new CancellationTokenSource();
			//CancellationToken token = source.Token;
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
					Setup.Conn.Close();
					Setup.Conn.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~BloggingContextDBFixture() {
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
		#endregion
	}

	public class BloggingContextDBFixture : BaseRepositoryTests, IDisposable
	{
		public BloggingContextDBFixture() : base()
		{
		}
	}

	public class BaseControllerTest
	{
		static string AssemblyDirectory
		{
			get
			{
				string codeBase = Assembly.GetExecutingAssembly().CodeBase;
				UriBuilder uri = new UriBuilder(codeBase);
				string path = Uri.UnescapeDataString(uri.Path);
				return Path.GetDirectoryName(path);
			}
		}

		protected ILoggerFactory LoggerFactory { get; private set; }

		protected IConfiguration Configuration { get; private set; }

		protected IDataProtectionProvider DataProtectionProvider { get; private set; }

		protected string ContentRoot { get; private set; }

		protected IConfiguration CreateConfiguration()
		{
			ContentRoot = Path.Combine(AssemblyDirectory, string.Format("..{0}..{0}..{0}..{0}AspNetCore.ExistingDb", Path.DirectorySeparatorChar.ToString()));

			var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			var builder = new ConfigurationBuilder()
				.SetBasePath(ContentRoot)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				//.AddJsonFile($@"appsettings.{env.EnvironmentName}.json", optional: true)
				.AddEnvironmentVariables();
			if (string.IsNullOrEmpty(env) || env == "Development")
				builder.AddUserSecrets<Startup>();
			return builder.Build();
		}

		protected virtual void SetupServices()
		{
			var serviceCollection = new ServiceCollection()
				.AddLogging();
			serviceCollection.AddDataProtection();

			var serviceProvider = serviceCollection.BuildServiceProvider();

			var factory = serviceProvider.GetService<ILoggerFactory>();
			LoggerFactory = factory;

			var protection = serviceProvider.GetService<IDataProtectionProvider>();
			DataProtectionProvider = protection;

			var configuration = CreateConfiguration();
			Configuration = configuration;

			if (string.IsNullOrEmpty(Configuration["LiveWebCamURL"]))
				Configuration["LiveWebCamURL"] = "https://127.0.0.1/webcamgalleryFake/Fakelive";
		}
	}
}
