using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Repositories
{
	public interface IBloggingRepository : IGenericRepository<BloggingContext, Blog>
	{
	}

	public class BloggingRepository : GenericRepository<BloggingContext, Blog>, IBloggingRepository
	{
		public BloggingRepository(BloggingContext context) : base(context)
		{
		}
	}
}
