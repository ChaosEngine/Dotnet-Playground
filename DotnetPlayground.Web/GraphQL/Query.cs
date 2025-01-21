using DotnetPlayground.GraphQL.Extensions;
using DotnetPlayground.Models;
using DotnetPlayground.Web.GraphQL.DataLoader;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetPlayground.GraphQL
{
	public class Query
	{
		[UseBloggingContext]
		[UsePaging]
		[HotChocolate.Data.UseProjection]
		[HotChocolate.Data.UseFiltering]
		[HotChocolate.Data.UseSorting]
		public IQueryable<Blog> GetBlogs(BloggingContext db)
		{
			return db.Blogs;
		}

		public Task<Blog> GetBlogAsync(int id, BlogByIdDataLoader dataLoader, CancellationToken cancellationToken) =>
			dataLoader.LoadAsync(id, cancellationToken);



		[UseBloggingContext]
		[UsePaging(MaxPageSize = 10000)]
		[HotChocolate.Data.UseProjection]
		[HotChocolate.Data.UseFiltering]
		[HotChocolate.Data.UseSorting]
		public IQueryable<ThinHashes> GetHashes(BloggingContext db)
		{
			return db.ThinHashes;
		}
	}
}
