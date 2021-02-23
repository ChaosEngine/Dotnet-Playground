using DotnetPlayground.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace DotnetPlayground.Repositories
{
	public interface IBloggingRepository : IGenericRepository<BloggingContext, Blog>
	{
		Task<bool> DeletePostAsync(int blogId, int postId);

		Task<List<Post>> GetPostsFromBlogAsync(int blogId);
	}

	public class BloggingRepository : GenericRepository<BloggingContext, Blog>, IBloggingRepository
	{
		public BloggingRepository(BloggingContext context) : base(context)
		{
		}

		public async Task<bool> DeletePostAsync(int blogId, int postId)
		{
			var post = await _entities.Posts.FirstOrDefaultAsync(p => p.BlogId == blogId && p.PostId == postId);
			if (post != null)
			{
				_entities.Remove(post);
				return true;
			}
			return false;
		}

		public async Task<List<Post>> GetPostsFromBlogAsync(int blogId)
		{
			var posts = await _entities.Posts.Where(p => p.BlogId == blogId).ToListAsync();
			return posts;
		}
	}
}
