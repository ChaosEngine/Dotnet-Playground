using AspNetCore.ExistingDb.Repositories;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
	public class Blogs
	{
		async Task<(SqliteConnection, DbContextOptions<BloggingContext>)> SetupInMemoryDB()
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

		[Fact]
		public async Task Add_writes_to_database()
		{
			// In-memory database only exists while the connection is open
			var (conn, db_opts) = await SetupInMemoryDB();
			try
			{
				// Run the test against one instance of the context
				using (var context = new BloggingContext(db_opts))
				{
					var repository = new BloggingRepository(context);

					await repository.AddAsync(new Blog { Url = "http://sample.com", BlogId = 1, Post = null });
					await repository.SaveAsync();
				}

				// Use a separate instance of the context to verify correct data was saved to database
				using (var context = new BloggingContext(db_opts))
				{
					Assert.Equal(1, await context.Blogs.CountAsync());
					Assert.Equal("http://sample.com", (await context.Blogs.SingleAsync()).Url);
				}
			}
			catch (Exception ex)
			{
				throw;
			}
			finally
			{
				conn.Dispose();
			}
		}

		[Fact]
		public async Task Find_searches_URL()
		{
			var (conn, db_opts) = await SetupInMemoryDB();
			try
			{
				// Insert seed data into the database using one instance of the context
				using (var context = new BloggingContext(db_opts))
				{
					var repository = new BloggingRepository(context);

					await repository.AddAsync(new Blog { Url = "http://sample.com/cats" });
					await repository.AddAsync(new Blog { Url = "http://sample.com/catfish" });
					await repository.AddAsync(new Blog { Url = "http://sample.com/dogs" });
					await repository.SaveAsync();
				}

				// Use a clean instance of the context to run the test
				using (var context = new BloggingContext(db_opts))
				{
					var repository = new BloggingRepository(context);
					var result = await repository.FindByAsync(b => b.Url.Contains("cat"));

					Assert.Equal(2, result.Count());
				}
			}
			catch (Exception ex)
			{
				throw;
			}
			finally
			{
				conn.Dispose();
			}
		}

		[Fact]
		public async Task Delete_by_URL()
		{
			var (conn, db_opts) = await SetupInMemoryDB();
			try
			{
				// Insert seed data into the database using one instance of the context
				using (var context = new BloggingContext(db_opts))
				{
					var repository = new BloggingRepository(context);

					await repository.AddAsync(new Blog { Url = "http://sample.com/foobar" });
					await repository.SaveAsync();
				}

				// Use a clean instance of the context to run the test
				using (var context = new BloggingContext(db_opts))
				{
					var repository = new BloggingRepository(context);
					var result = (await repository.GetAllAsync()).First();
					repository.Delete(result);

					await repository.SaveAsync();

					Assert.Equal((await repository.GetAllAsync()).Count, 0);
				}
			}
			catch (Exception ex)
			{
				throw;
			}
			finally
			{
				conn.Dispose();
			}
		}

		[Fact]
		public async Task Edit()
		{
			var (conn, db_opts) = await SetupInMemoryDB();
			try
			{
				// Insert seed data into the database using one instance of the context
				using (var context = new BloggingContext(db_opts))
				{
					var repository = new BloggingRepository(context);

					await repository.AddAsync(new Blog { Url = "http://sample.com/foo" });
					await repository.SaveAsync();
				}

				// Use a clean instance of the context to run the test
				using (var context = new BloggingContext(db_opts))
				{
					var repository = new BloggingRepository(context);
					var result = (await repository.FindByAsync(x => x.Url == "http://sample.com/foo")).FirstOrDefault();
					Assert.NotNull(result);

					result.Url = "http://domain.com/bar";
					repository.Edit(result);

					await repository.SaveAsync();
				}

				using (var context = new BloggingContext(db_opts))
				{
					var repository = new BloggingRepository(context);
					var result = await repository.FindByAsync(x => x.Url != null);
					Assert.Equal(result.First().Url, "http://domain.com/bar");
				}
			}
			catch (Exception ex)
			{
				throw;
			}
			finally
			{
				conn.Dispose();
			}
		}
	}
}
