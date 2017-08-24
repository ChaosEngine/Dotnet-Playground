using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Tests
{
	public abstract class BaseTests
	{
		protected async Task<(SqliteConnection, DbContextOptions<BloggingContext>)> SetupInMemoryDB()
		{
			var connection = new SqliteConnection("DataSource=:memory:");

			await connection.OpenAsync();

			var options = new DbContextOptionsBuilder<BloggingContext>()
				.UseSqlite(connection)
				.Options;

			// Create the schema in the database
			using (var context = new BloggingContext(options))
			{
				await context.Database.EnsureCreatedAsync();
			}

			return (connection, options);
		}
	}
}
