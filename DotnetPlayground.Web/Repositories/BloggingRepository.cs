using DotnetPlayground.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using System.Diagnostics.CodeAnalysis;

namespace DotnetPlayground.Repositories
{
	public interface IBloggingRepository : IGenericRepository<BloggingContext, Blog>
	{
		Task<bool> DeletePostAsync(Expression<Func<Post, bool>> predicate);

		Task<List<Post>> GetPostsFromBlogAsync(int blogId);

		Task<int> EditPosts(Expression<Func<Post, bool>> predicate,
			Expression<Func<SetPropertyCalls<Post>, SetPropertyCalls<Post>>> setPropertyCalls);
	}

	[RequiresUnreferencedCode("Using EF with _entities.Set<Ent> generic method")]
	public class BloggingRepository : GenericRepository<BloggingContext, Blog>, IBloggingRepository
	{
		public BloggingRepository(BloggingContext context) : base(context)
		{
		}

		public async Task<bool> DeletePostAsync(Expression<Func<Post, bool>> predicate)
		{
			var deleted_count = await _entities.Posts
				.Where(predicate)
				.ExecuteDeleteAsync();
			if (deleted_count > 0)
				return true;
			return false;
		}

		public async Task<List<Post>> GetPostsFromBlogAsync(int blogId)
		{
			var posts = await _entities.Posts.Where(p => p.BlogId == blogId).ToListAsync();
			return posts;
		}

        public async Task<int> EditPosts(Expression<Func<Post, bool>> predicate,
			Expression<Func<SetPropertyCalls<Post>, SetPropertyCalls<Post>>> setPropertyCalls)
        {
            var updated_count = await _entities.Posts
                .Where(predicate)
                .ExecuteUpdateAsync(setPropertyCalls);

            return updated_count;
        }
    }
}
