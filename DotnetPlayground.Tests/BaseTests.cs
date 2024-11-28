using DotnetPlayground.Models;
using Lib.ServerTiming;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DotnetPlayground.Tests
{
	public abstract class BaseRepositoryTests : IDisposable
	{
		public (SqliteConnection Conn,
				DbContextOptions<BloggingContext> DbOpts,
				IConfiguration Conf, IMemoryCache Cache,
				ILogger<Repositories.HashesRepository> Logger,
				IServerTiming ServerTiming
				) Setup
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

		protected async Task<(SqliteConnection,
								DbContextOptions<BloggingContext>,
								IConfiguration,
								IMemoryCache,
								ILogger<Repositories.HashesRepository>,
								IServerTiming)> SetupInMemoryDB()
		{
			var builder = new ConfigurationBuilder()
				//.AddJsonFile("config.json", optional: false, reloadOnChange: true)
				.AddInMemoryCollection(new Dictionary<string, string>
				{
					{ "ConnectionStrings:Sqlite", "DataSource=:memory:" },
					{ "DBKind", "sqlite" }
				});
			var config = builder.Build();

			var connection = new SqliteConnection(config.GetConnectionString("Sqlite"));

			// In-memory database only exists while the connection is open
			await connection.OpenAsync();

			var options = new DbContextOptionsBuilder<BloggingContext>()
				.UseSqlite(connection)
				.ConfigureWarnings(b =>
					b.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)
				)
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

			var serverTiming_mock = new Moq.Mock<IServerTiming>();
			serverTiming_mock.SetupGet(m => m.Metrics).Returns(() =>
			{
				return new List<Lib.ServerTiming.Http.Headers.ServerTimingMetric>();
			});

			return (connection, options, config, cache, logger, serverTiming_mock.Object);
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
		protected ILoggerFactory LoggerFactory { get; private set; }

		protected IConfiguration Configuration { get; private set; }

		protected IDataProtectionProvider DataProtectionProvider { get; private set; }

		protected IMemoryCache Cache { get; private set; }

		protected string ContentRoot { get; private set; }

		protected IConfiguration CreateConfiguration()
		{
			ContentRoot = Integration.TestServerFixture<DotnetPlayground.Startup>.GetProjectPath<DotnetPlayground.Startup>();

			var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			var builder = new ConfigurationBuilder()
				.SetBasePath(ContentRoot)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				//.AddJsonFile($@"appsettings.{env.EnvironmentName}.json", optional: true)
				.AddEnvironmentVariables();
			if (string.IsNullOrEmpty(env) || env == "Development")
				builder.AddUserSecrets<Startup>(optional: true);
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

			var cache = serviceProvider.GetService<IMemoryCache>();
			Cache = cache;

			if (string.IsNullOrEmpty(Configuration["LiveWebCamURL"]))
				Configuration["LiveWebCamURL"] = "https://127.0.0.1/webcamgalleryFake/Fakelive";
		}
	}

	#region Authorized tests

	public sealed class AuthorizedTestingStartup
		: InkBall.IntegrationTests.BaseTestingStartup<ApplicationUser, Helpers.MySignInManager>
	{
		public AuthorizedTestingStartup(IConfiguration configuration) : base(configuration)
		{
		}
	}

	public sealed class AuthorizedTestServerFixture
		: InkBall.IntegrationTests.BaseTestServerFixture<AuthorizedTestingStartup, ApplicationUser>
	{
		public override string DesiredContentRoot
		{
			get
			{
				var contentRoot = Integration.TestServerFixture<Startup>.GetProjectPath<Startup>();
				return contentRoot;
			}
		}
	}

	[CollectionDefinition(nameof(AuthorizedTestingServerCollection))]
	public sealed class AuthorizedTestingServerCollection : ICollectionFixture<AuthorizedTestServerFixture>
	{
		// This class has no code, and is never created. Its purpose is simply
		// to be the place to apply [CollectionDefinition] and all the
		// ICollectionFixture<> interfaces.
	}

	#endregion Authorized tests
}
