﻿using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.ExistingDb.Tests;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace Hashes
{
	public class HashesRepository : BaseTests, IDisposable
	{
		public HashesRepository() : base()
		{
		}

		[Fact]
		public async Task Empty_Hashes()
		{
			try
			{
				// Run the test against one instance of the context
				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
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
		}

		[Fact]
		public async Task SomeValues()
		{
			try
			{
				// Run the test against one instance of the context
				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
					await repository.AddAsync(new ThinHashes
					{
						Key = "alamakota",
						HashMD5 = "dc246bcdd6cb3548579770a034d2e678",
						HashSHA256 = "63b347973bb99fed9277b33cb4646b205e9a31331acfa574add3d2351f445e43"
					});
					await repository.SaveAsync();
				}

				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
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
		}

		[Fact]
		public async Task AutoComplete()
		{
			try
			{
				// Run the test against one instance of the context
				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
					await repository.AddRangeAsync(new[]{
						new ThinHashes
						{
							Key = "alamakota",
							HashMD5 = "dc246bcdd6cb3548579770a034d2e678",
							HashSHA256 = "63b347973bb99fed9277b33cb4646b205e9a31331acfa574add3d2351f445e43"
						},
						new ThinHashes
						{
							Key = "fakefakef",
							HashMD5 = "fakefakefakefakefakefakefakefake",
							HashSHA256 = "fakefakefakefakefakefakefakefakefakefakefakefakefakefakefakefake"
						}
					});
					await repository.SaveAsync();
				}

				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
					var found = await repository.AutoComplete("NOEXIST");
					Assert.NotNull(found);
					Assert.NotEmpty(found);
					Assert.Equal(found.First().Key, "nothing found");

					found = await repository.AutoComplete("fake");
					Assert.NotNull(found);
					Assert.NotEmpty(found);
					Assert.Contains(found.First().Key, "fakefakef");

					found = await repository.AutoComplete("63b347973bb99");
					Assert.NotNull(found);
					Assert.NotEmpty(found);
					Assert.Contains(found.First().Key, "alamakota");

					found = await repository.AutoComplete("fakefakefakefakefakefakefake");
					Assert.NotNull(found);
					Assert.NotEmpty(found);
					Assert.Contains(found.First().Key, "fakefakef");
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		[Fact]
		public async Task Search_NOT_FOUND()
		{
			try
			{
				// Run the test against one instance of the context
				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
					await repository.AddRangeAsync(new[]{
						new ThinHashes
						{
							Key = "alamakota",
							HashMD5 = "dc246bcdd6cb3548579770a034d2e678",
							HashSHA256 = "63b347973bb99fed9277b33cb4646b205e9a31331acfa574add3d2351f445e43"
						},
						new ThinHashes
						{
							Key = "fakefakef",
							HashMD5 = "fakefakefakefakefakefakefakefake",
							HashSHA256 = "fakefakefakefakefakefakefakefakefakefakefakefakefakefakefakefake"
						}
					});
					await repository.SaveAsync();
				}

				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
					var found = await repository.SearchAsync("Key", "desc", "dummy", 0, 10);
					Assert.NotNull(found);
					Assert.NotEmpty(found.Itemz);
					Assert.Equal(found.Itemz.Count(), 1);
					Assert.Equal(found.Itemz.First().Key, "nothing found");
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		[Fact]
		public async Task Search_FOUND()
		{
			try
			{
				// Run the test against one instance of the context
				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
					await repository.AddRangeAsync(new[]{
						new ThinHashes
						{
							Key = "alamakota",
							HashMD5 = "dc246bcdd6cb3548579770a034d2e678",
							HashSHA256 = "63b347973bb99fed9277b33cb4646b205e9a31331acfa574add3d2351f445e43"
						},
						new ThinHashes
						{
							Key = "fakefakef",
							HashMD5 = "fakefakefakefakefakefakefakefake",
							HashSHA256 = "fakefakefakefakefakefakefakefakefakefakefakefakefakefakefakefake"
						}
					});
					await repository.SaveAsync();
				}

				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
					var found = await repository.SearchAsync("Key", "desc", "fake", 0, 10);
					Assert.NotNull(found);
					Assert.NotEmpty(found.Itemz);
					Assert.Equal(found.Itemz.Count(), 1);
					Assert.Equal(found.Itemz.First().Key, "fakefakef");
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		[Theory]
		[InlineData(100)]
		public async Task Paging_lots_of_elements(int itemsCount)
		{
			try
			{
				// Run the test against one instance of the context
				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
					var tasks = new List<Task>(itemsCount + 1);
					for (int i = 0; i < itemsCount; i++)
					{
						tasks.Add(repository.AddRangeAsync(new[]{
							new ThinHashes
							{
								Key = $"alamakota_{i}",
								HashMD5 = $"dc246bcdd6cb3548579770a034d2e678_{i}",
								HashSHA256 = $"63b347973bb99fed9277b33cb4646b205e9a31331acfa574add3d2351f445e43_{i}"
							},
							new ThinHashes
							{
								Key = $"fakefakef_{i}",
								HashMD5 = $"fakefakefakefakefakefakefakefake_{i}",
								HashSHA256 = $"fakefakefakefakefakefakefakefakefakefakefakefakefakefakefakefake_{i}"
							}
						}));
					}
					Task.WaitAll(tasks.ToArray());
					await repository.SaveAsync();
				}

				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
					var found = await repository.SearchAsync("Key", "asc", "fake", 2, 10);
					Assert.NotNull(found);
					Assert.NotEmpty(found.Itemz);
					Assert.Equal(found.Count, itemsCount);
					Assert.Equal(found.Itemz.Count(), 10);
					Assert.Equal(found.Itemz.First().Key, "fakefakef_10");
					Assert.Equal(found.Itemz.First().HashSHA256, "fakefakefakefakefakefakefakefakefakefakefakefakefakefakefakefake_10");
				}
			}
			catch (Exception)
			{
				throw;
			}
			finally
			{
				Setup.Conn.Dispose();
			}

			//2nd run
			var db = SetupInMemoryDB();
			db.Wait();
			Setup = db.Result;
			try
			{
				// Run the test against one instance of the context
				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
					var tasks = new List<Task>(itemsCount + 1);
					for (int i = 0; i < itemsCount; i++)
					{
						tasks.Add(repository.AddRangeAsync(new[]{
							new ThinHashes
							{
								Key = $"alamakota_{i}",
								HashMD5 = $"dc246bcdd6cb3548579770a034d2e678_{i}",
								HashSHA256 = $"63b347973bb99fed9277b33cb4646b205e9a31331acfa574add3d2351f445e43_{i}"
							},
							new ThinHashes
							{
								Key = $"fakefakef_{i}",
								HashMD5 = $"fakefakefakefakefakefakefakefake_{i}",
								HashSHA256 = $"fakefakefakefakefakefakefakefakefakefakefakefakefakefakefakefake_{i}"
							}
						}));
					}
					Task.WaitAll(tasks.ToArray());
					await repository.SaveAsync();
				}

				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
					var found = await repository.SearchAsync("Key", "asc", "63b347973bb99f", 2, 10);
					Assert.NotNull(found);
					Assert.NotEmpty(found.Itemz);
					Assert.Equal(found.Count, itemsCount);
					Assert.Equal(found.Itemz.Count(), 10);
					Assert.Equal(found.Itemz.First().Key, "alamakota_10");
					Assert.Equal(found.Itemz.First().HashSHA256, "63b347973bb99fed9277b33cb4646b205e9a31331acfa574add3d2351f445e43_10");
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		[Fact]
		public async Task CalculateHashesInfo()
		{
			try
			{
				// Run the test against one instance of the context
				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
					int count = 100;
					var tasks = new List<Task>(count + 1);
					for (int i = 0; i < count; i++)
					{
						tasks.Add(repository.AddRangeAsync(new[]{
							new ThinHashes
							{
								Key = $"alamakota_{i}",
								HashMD5 = $"dc246bcdd6cb3548579770a034d2e678_{i}",
								HashSHA256 = $"63b347973bb99fed9277b33cb4646b205e9a31331acfa574add3d2351f445e43_{i}"
							},
							new ThinHashes
							{
								Key = $"bakebakeb_{i}",
								HashMD5 = $"bakebakebakebakebakebakebakebake_{i}",
								HashSHA256 = $"bakebakebakebakebakebakebakebakebakebakebakebakebakebakebakebake_{i}"
							},
							new ThinHashes
							{
								Key = $"cakecakec_{i}",
								HashMD5 = $"cakecakecakecakecakecakecakecake_{i}",
								HashSHA256 = $"cakecakecakecakecakecakecakecakecakecakecakecakecakecakecakecake_{i}"
							},

						}));
					}
					Task.WaitAll(tasks.ToArray());
					await repository.SaveAsync();
				}

				using (var context = new BloggingContext(Setup.DbOpts))
				{
					var repository = new AspNetCore.ExistingDb.Repositories.HashesRepository(context, Setup.Conf);
					var factory = new LoggerFactory();
					var logger = factory.CreateLogger<HashesRepository>();
					var res0 = await repository.CalculateHashesInfo(factory, logger, Setup.Conf, Setup.DbOpts);

					Assert.NotNull(res0);

					var res1 = await repository.CalculateHashesInfo(factory, logger, Setup.Conf, Setup.DbOpts);

					Assert.Same(res0, res1);
					Assert.Equal(res0.Alphabet, "abc");
					Assert.Equal(res0.Count, 300);
					Assert.Equal(res0.IsCalculating, false);
					Assert.Equal(res0.KeyLength, 12);

					var expected = await repository.CurrentHashesInfo;

					Assert.Equal(res1.ID, expected.ID);
					Assert.Equal(res1.Alphabet, expected.Alphabet);
					Assert.Equal(res1.Count, expected.Count);
					Assert.Equal(res1.IsCalculating, expected.IsCalculating);
					Assert.Equal(res1.KeyLength, expected.KeyLength);
					Assert.Equal(res1.ToString(), expected.ToString());
				}
			}
			catch (Exception)
			{
				throw;
			}
		}
	}
}
