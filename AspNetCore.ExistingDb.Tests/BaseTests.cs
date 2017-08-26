using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Tests
{
	public abstract class BaseTests : IDisposable
	{
		public (SqliteConnection Conn, DbContextOptions<BloggingContext> DbOpts, IConfiguration Conf) Setup
		{
			get; set;
		}

		protected async Task<(SqliteConnection, DbContextOptions<BloggingContext>, IConfiguration)> SetupInMemoryDB()
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

			return (connection, options, config);
		}

		public BaseTests()
		{
			var db = SetupInMemoryDB();
			db.Wait();
			Setup = db.Result;
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

	public class BloggingContextDBFixture : BaseTests, IDisposable
	{
		public BloggingContextDBFixture() : base()
		{
		}
	}
}
