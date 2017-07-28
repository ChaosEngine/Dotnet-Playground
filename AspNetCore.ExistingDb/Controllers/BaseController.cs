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

		protected readonly ILogger<BaseController<T>> _logger;

		public static IEnumerable<string> AllColumnNames => _allColumnNames;

		public BaseController(ILogger<BaseController<T>> logger)
		{
			_logger = logger;
		}

		protected string ItemsToJson(IQueryable<T> items, IEnumerable<string> columnNames, string sort, string order, int limit, int offset)
		{
			try
			{
				// where clause is set, count total records
				int count = items.Count();

				// Skip requires sorting, so make sure there is always sorting
				string sortExpression;

				if (sort?.Length > 0)
					sortExpression = string.Format("{0} {1}", sort, order);
				else
					sortExpression = string.Empty;

				// show ALL records if limit is not set
				if (limit == 0)
					limit = count;

				// Prepare json structure
				var result = new
				{
					total = count,
					rows = items.OrderBy(sortExpression).Skip(offset).Take(limit)//.Select("new (" + string.Join(',', columnNames) + ")")
				};

				return JsonConvert.SerializeObject(result, Formatting.None,
					new JsonSerializerSettings() { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.Message);
				return null;
			}
		}

		// needs System.Linq.Dynamic.Core
		protected IQueryable<T> SearchItems(IQueryable<T> items, string search, IEnumerable<string> columnNames)
		{
			// Apply filtering to all visible column names
			if (search?.Length > 0)
			{
				StringBuilder sb = new StringBuilder(1000);
				// Create dynamic Linq expression
				string comma = string.Empty;
				foreach (string fieldName in columnNames)
				{
					sb.AppendFormat("{1}({0} == null ? false : {0}.ToString().IndexOf(@0, @1) >=0)", fieldName, comma);
					comma = " or\r\n";
				}
				// Get search expression
				string searchExpression = sb.ToString();
				// Apply filtering, 
				items = items.Where(searchExpression, search, StringComparison.OrdinalIgnoreCase);

				//items = items.Where(Filteradoo).AsQueryable();
			}

			return items;

			//inner method
			//bool Filteradoo(T arg)
			//{
			//	foreach (var col in columnNames)
			//	{
			//		var value = typeof(T).GetProperty(col,
			//			BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)
			//			.GetValue(arg);

			//		if (value?.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
			//			return true;
			//	}

			//	return false;
			//}
		}
	}
}
