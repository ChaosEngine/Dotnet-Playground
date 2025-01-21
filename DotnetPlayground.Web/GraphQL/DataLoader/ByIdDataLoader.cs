using DotnetPlayground.Models;
using GreenDonut;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetPlayground.Web.GraphQL.DataLoader
{
	public class BlogByIdDataLoader : BatchDataLoader<int, Blog>
	{
		private readonly IDbContextFactory<BloggingContext> _dbContextFactory;

		public BlogByIdDataLoader(
			IBatchScheduler batchScheduler,
			DataLoaderOptions options,
			IDbContextFactory<BloggingContext> dbContextFactory)
			: base(batchScheduler, options)
		{
			_dbContextFactory = dbContextFactory ??
				throw new ArgumentNullException(nameof(dbContextFactory));
		}

		protected override async Task<IReadOnlyDictionary<int, Blog>> LoadBatchAsync(
			IReadOnlyList<int> keys,
			CancellationToken cancellationToken)
		{
			await using BloggingContext dbContext =
				_dbContextFactory.CreateDbContext();

			return await dbContext.Blogs
				.Where(s => keys.Contains(s.BlogId))
				.ToDictionaryAsync(t => t.BlogId, cancellationToken);
		}
	}
}
