using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetPlayground
{
	public class PaginatedList<T> : List<T>
	{
		public int Offset { get; }

		public int PageSize { get; }

		public int FoundCount { get; }

		public int PagesCount
		{
			get { return FoundCount / PageSize; }
		}

		public PaginatedList(IEnumerable<T> items, int foundCount, int offset, int pageSize)
			: base(items.Count())
		{
			Offset = offset;
			PageSize = pageSize;
			FoundCount = foundCount;

			base.AddRange(items);
		}

		public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int offset, int pageSize,
			CancellationToken token)
		{
			var count = await source.CountAsync(token);
			var items = await source.Skip(offset).Take(pageSize).ToListAsync(token);

			return new PaginatedList<T>(items, count, offset, pageSize);
		}
	}
}
