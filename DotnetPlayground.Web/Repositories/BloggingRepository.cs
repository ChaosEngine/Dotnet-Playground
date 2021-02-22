using DotnetPlayground.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace DotnetPlayground.Repositories
{
	public interface IBloggingRepository : IGenericRepository<BloggingContext, Blog>
	{
		Task<bool> DeletePostAsync(int blogId, int postId);

		Task<Blog> GetBlogWithPostsAsync(int blogId);
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

		public async Task<Blog> GetBlogWithPostsAsync(int blogId)
		{
			var blog = await _entities.Blogs.Include(b => b.Post).FirstOrDefaultAsync(p => p.BlogId == blogId);
			return blog;
		}
	}
}
