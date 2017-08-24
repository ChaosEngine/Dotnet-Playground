using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.ExistingDb.Tests;
using Xunit;

namespace Hashes
{
	public class HashesRepository : BaseTests
	{
		[Fact]
		public async Task Empty_Hashes()
		{
			// In-memory database only exists while the connection is open
			var (conn, db_opts) = await SetupInMemoryDB();
			try
			{
				IConfiguration configuration = null;
				// Run the test against one instance of the context
				using (var context = new BloggingContext(db_opts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, configuration);
					var all = await repository.GetAllAsync();

					Assert.Empty(all);

					var found = await repository.FindByAsync(x => true);
					Assert.Empty(found);
				}
			}
			catch (Exception)
			{
				throw;
			}
			finally
			{
				conn.Dispose();
			}
		}

		[Fact]
		public async Task SomeValues()
		{
			// In-memory database only exists while the connection is open
			var (conn, db_opts) = await SetupInMemoryDB();
			try
			{
				IConfiguration configuration = null;
				// Run the test against one instance of the context
				using (var context = new BloggingContext(db_opts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, configuration);
					await repository.AddAsync(new ThinHashes
					{
						Key = "alamakota",
						HashMD5 = "dc246bcdd6cb3548579770a034d2e678",
						HashSHA256 = "63b347973bb99fed9277b33cb4646b205e9a31331acfa574add3d2351f445e43"
					});
					await repository.SaveAsync();
				}

				using (var context = new BloggingContext(db_opts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, configuration);
					var found = await repository.FindByAsync(x => x.HashSHA256 == "63b347973bb99fed9277b33cb4646b205e9a31331acfa574add3d2351f445e43");
					Assert.NotNull(found);
					Assert.NotEmpty(found);
					Assert.Equal(found.First().Key, "alamakota");
				}
			}
			catch (Exception)
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
