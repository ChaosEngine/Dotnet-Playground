using DotnetPlayground.GraphQL.Extensions;
using DotnetPlayground.Models;
using HotChocolate;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetPlayground.GraphQL
{
	public record AddBlogInput(string Url);

	public record DeletePayload(string status);

	public class AddBlogPayload
	{
		public Blog Blog { get; }

		public AddBlogPayload(Blog blog)
		{
			Blog = blog;
		}
	}

	public class Mutation
	{
		[UseBloggingContext]
		public async Task<AddBlogPayload> AddBlogAsync(
			AddBlogInput input,
			[Service(ServiceKind.Resolver)] BloggingContext context,
			CancellationToken cancellationToken
			)
		{
			var blog = new Blog
			{
				Url = input.Url,
				//Post = input.Post
			};

			context.Blogs.Add(blog);
			await context.SaveChangesAsync(cancellationToken);

			return new AddBlogPayload(blog);
		}

		[UseBloggingContext]
		public async Task<DeletePayload> DeleteBlogAsync(
			IReadOnlyList<int> ids,
			[Service(ServiceKind.Resolver)] BloggingContext context,
			CancellationToken cancellationToken
		)
		{
			var for_delete = await context.Blogs
				.Where(s => ids.Contains(s.BlogId))
				.ToListAsync(cancellationToken);

			if (for_delete.Count > 0)
			{
				context.Blogs.RemoveRange(for_delete);
				await context.SaveChangesAsync(cancellationToken);
				return new DeletePayload($"deleted {for_delete.Count}");
			}
			else
				return new DeletePayload("not found");
		}
	}
}
