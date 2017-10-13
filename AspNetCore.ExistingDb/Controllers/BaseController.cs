using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text;

namespace EFGetStarted.AspNetCore.ExistingDb.Controllers
{
	public class BaseController<T> : Controller where T : class
	{
		private static readonly IEnumerable<string> _allColumnNames =
			typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToArray();

		public static IEnumerable<string> AllColumnNames => _allColumnNames;

		public static (IEnumerable<T> Itemz, int Count) ItemsToJson(IQueryable<T> items, string sort, string order, int limit, int offset)
		{
			// where clause is set, count total records
			int count = items.Count();
			
			if (sort?.Length > 0)
			{
				// Skip requires sorting, so make sure there is always sorting
				string sortExpression = string.Format("{0} {1}", sort, order);
				items = items.OrderBy(sortExpression);
			}
		
			// show ALL records if limit is not set
			if (limit <= 0)
				limit = count;
						
			items = items.Skip(offset).Take(limit);

			return (items, items.Count());
		}

		// needs System.Linq.Dynamic.Core
		public static IQueryable<T> SearchItems(IQueryable<T> items, string search, IEnumerable<string> columnNames)
		{
			// Apply filtering to all visible column names
			if (search?.Length > 0)
			{
				/*StringBuilder sb = new StringBuilder(1000);
				// Create dynamic Linq expression
				string comma = string.Empty;
				foreach (string fieldName in columnNames)
				{
					//sb.AppendFormat("{1}({0} == null ? false : {0}.ToString().IndexOf(@0, @1) >=0)", fieldName, comma);
					sb.AppendFormat("{1}({0} == null ? false : {0}.ToString().StartsWith(@0, @1))", fieldName, comma);
					comma = " or\r\n";
				}
				// Get search expression
				string searchExpression = sb.ToString();
				// Apply filtering, 
				items = items.Where(searchExpression, search, StringComparison.OrdinalIgnoreCase);*/

				items = items.Where(FilteraDoo).AsQueryable();
			}

			return items;

			//inner method
			bool FilteraDoo(T arg)
			{
				foreach (var col in columnNames)
				{
					object value = typeof(T).GetProperty(col,
						BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)
						.GetValue(arg);

					//if (value?.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
					if (value != null && value.ToString().StartsWith(search, StringComparison.OrdinalIgnoreCase))
						return true;
				}

				return false;
			}
		}
	}
}
