using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AspNetCore.ExistingDb.Controllers
{
	public class BaseController<T> : Controller where T : class
	{
		private static readonly IEnumerable<string> _allColumnNames =
			typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToArray();

		public static IEnumerable<string> AllColumnNames => _allColumnNames;

		public static (IEnumerable<T> Itemz, int Count) ItemsToJson(IEnumerable<T> items, string sort, string order, int limit, int offset)
		{
			if (sort?.Length > 0)
			{
				// Skip requires sorting, so make sure there is always sorting
				//string sortExpression = string.Format("{0} {1}", sort, order);
				//items = items.OrderBy(sortExpression);
				if (order.ToUpperInvariant() == "ASC")
					items = items.OrderBy(OrderaDoo);
				else
					items = items.OrderByDescending(OrderaDoo);
			}

			if (limit <= 0)
				limit = items.Count();

			items = items.Skip(offset).Take(limit);

			return (items, items.Count());

			//inner method
			object OrderaDoo(T arg)
			{
				object value = typeof(T).GetProperty(sort,
					BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)
					.GetValue(arg);
				return value;
			}
		}

		// needs System.Linq.Dynamic.Core
		public static IEnumerable<T> SearchItems(IQueryable<T> items, string search, IEnumerable<string> columnNames)
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

				return items.Where(FilteraDoo);
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
