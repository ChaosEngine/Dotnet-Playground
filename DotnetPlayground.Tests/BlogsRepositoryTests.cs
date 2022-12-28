using DotnetPlayground.Repositories;
using DotnetPlayground.Tests;
using DotnetPlayground.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Repositories
{
	public class BlogsRepository : IClassFixture<BloggingContextDBFixture>
	{
		BloggingContextDBFixture DBFixture { get; set; }

		public BlogsRepository(BloggingContextDBFixture dBFixture)
		{
			DBFixture = dBFixture;
		}

		[Fact]
		public async Task Add_writes_to_database()
		{
			try
			{
				// Run the test against one instance of the context
				using (var context = new BloggingContext(DBFixture.Setup.DbOpts))
				{
					var repository = new BloggingRepository(context);

					await repository.AddAsync(new Blog { Url = "http://sample.com", /*BlogId = 1,*/ Post = null });
					await repository.SaveAsync();
				}

				// Use a separate instance of the context to verify correct data was saved to database
				using (var context = new BloggingContext(DBFixture.Setup.DbOpts))
				{
					Assert.Equal(1, await context.Blogs.CountAsync(x => x.Url == "http://sample.com"));
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		[Fact]
		public async Task Find_searches_URL()
		{
			try
			{
				// Insert seed data into the database using one instance of the context
				using (var context = new BloggingContext(DBFixture.Setup.DbOpts))
				{
					var repository = new BloggingRepository(context);

					await repository.AddAsync(new Blog { Url = "http://sample.com/cats" });
					await repository.AddAsync(new Blog { Url = "http://sample.com/catfish" });
					await repository.AddAsync(new Blog { Url = "http://sample.com/dogs" });
					await repository.SaveAsync();
				}

				// Use a clean instance of the context to run the test
				using (var context = new BloggingContext(DBFixture.Setup.DbOpts))
				{
					var repository = new BloggingRepository(context);
					var result = await repository.FindByAsync(b => b.Url.Contains("cat"));

					Assert.Equal(2, result.Count());
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		[Fact]
		public async Task Delete_by_URL()
		{
			try
			{
				// Insert seed data into the database using one instance of the context
				using (var context = new BloggingContext(DBFixture.Setup.DbOpts))
				{
					var repository = new BloggingRepository(context);

					await repository.AddAsync(new Blog { Url = "http://sample.com/foobar" });
					await repository.SaveAsync();
				}

				// Use a clean instance of the context to run the test
				using (var context = new BloggingContext(DBFixture.Setup.DbOpts))
				{
					var repository = new BloggingRepository(context);
					await repository.Delete(x => x.Url == "http://sample.com/foobar");

					Assert.Empty((await repository.FindByAsync(x => x.Url == "http://sample.com/foobar")));
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		[Fact]
		public async Task Edit()
		{
			try
			{
				// Insert seed data into the database using one instance of the context
				using (var context = new BloggingContext(DBFixture.Setup.DbOpts))
				{
					var repository = new BloggingRepository(context);

					await repository.AddAsync(new Blog { Url = "http://some.address.com/foo" });
					await repository.SaveAsync();
				}

				// Use a clean instance of the context to run the test
				using (var context = new BloggingContext(DBFixture.Setup.DbOpts))
				{
					var repository = new BloggingRepository(context);
					
					await repository.Edit(
						x => x.Url == "http://some.address.com/foo", 
						b=> b.SetProperty(blob => blob.Url, "http://domain.com/bar")
					);

					await repository.SaveAsync();
				}

				using (var context = new BloggingContext(DBFixture.Setup.DbOpts))
				{
					var repository = new BloggingRepository(context);
					var result = await repository.FindByAsync(x => x.Url.StartsWith("http://domain.com"));
					Assert.Equal("http://domain.com/bar", result.First().Url);
				}
			}
			catch (Exception)
			{
				throw;
			}
		}
	}
}
