using DotnetPlayground.Models;
using DotnetPlayground.Web.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Text.Json;

namespace DotnetPlayground
{
	public static class ConfigurationExtensions
	{
		public static string AppRootPath(this IConfiguration configuration)
		{
			return configuration["AppRootPath"];
		}
	}

	public static class SessionExtensions
	{
		//public static void Set<T>(this ISession session, string key, T value)
		//{
		//	session.SetString(key, JsonSerializer.Serialize(value));
		//}

		//public static T Get<T>(this ISession session, string key)
		//{
		//	var value = session.GetString(key);

		//	return value == null ?
		//		default(T) :
		//		JsonSerializer.Deserialize<T>(value);
		//}

		public static void Set(this ISession session, string key, DateTime value)
		{
			session.SetString(key, JsonSerializer.Serialize(value, DateTime_Context.Default.DateTime));
		}

		public static DateTime GetDateTime(this ISession session, string key)
		{
			var value = session.GetString(key);

			return value == null ?
				default(DateTime) :
				JsonSerializer.Deserialize(value, DateTime_Context.Default.DateTime);
		}

		public static void Set(this ISession session, string key, RandomData value)
		{
			session.SetString(key, JsonSerializer.Serialize(value, RandomData_Context.Default.RandomData));
		}

		public static RandomData GetRandomData(this ISession session, string key)
		{
			var value = session.GetString(key);

			return value == null ?
				default(RandomData) :
				JsonSerializer.Deserialize(value, RandomData_Context.Default.RandomData);
		}
	}

	//public static class TempDataExtensions
	//{
	//	public static void Put<T>(this ITempDataDictionary tempData, string key, T value) where T : class
	//	{
	//		tempData[key] = JsonSerializer.Serialize(value);
	//	}

	//	public static T Get<T>(this ITempDataDictionary tempData, string key) where T : class
	//	{
	//		object value;
	//		tempData.TryGetValue(key, out value);
	//		return value == null ?
	//			default(T) :
	//			JsonSerializer.Deserialize<T>((string)value);
	//	}
	//}

	public sealed class DBConfigShower
	{
		public string DBConfig { get; set; }

		public DBConfigShower()
		{
			//DBConfig = "<null>";
		}
	}

	static class MvcOptionsExtensions
	{
		class RouteConvention<T> : IApplicationModelConvention
			where T : Controller
		{
			private readonly IRouteTemplateProvider _routeTemplateProvider;

			public RouteConvention(IRouteTemplateProvider routeTemplateProvider)
			{
				_routeTemplateProvider = routeTemplateProvider;
			}

			public void Apply(ApplicationModel application)
			{
				var matchedSelectors = application.Controllers.FirstOrDefault(c => c.ControllerType == typeof(T))?.Selectors;
				if (matchedSelectors != null && matchedSelectors.Any())
				{
					var centralPrefix = new AttributeRouteModel(_routeTemplateProvider);
					foreach (var selectorModel in matchedSelectors)
					{
						selectorModel.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(centralPrefix,
							selectorModel.AttributeRouteModel);
					}
				}
			}
		}

		/*class PageConvention : IPageConvention
		{
			private readonly IRouteTemplateProvider _routeTemplateProvider;

			public PageConvention(IRouteTemplateProvider routeTemplateProvider)
			{
				_routeTemplateProvider = routeTemplateProvider;
			}

			public void Apply(ApplicationModel application)
			{
				var matchedSelectors = application.Controllers.FirstOrDefault(c => c.ControllerType == typeof(PageController))?.Selectors;
				if (matchedSelectors != null && matchedSelectors.Any())
				{
					var centralPrefix = new AttributeRouteModel(_routeTemplateProvider);
					foreach (var selectorModel in matchedSelectors)
					{
						selectorModel.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(centralPrefix,
							selectorModel.AttributeRouteModel);
					}
				}
			}
		}*/

		public static void UseCentralRoutePrefix<T>(this MvcOptions opts, IRouteTemplateProvider routeAttribute)
			where T : Controller
		{
			opts.Conventions.Insert(0, new RouteConvention<T>(routeAttribute));
		}

		/*public static void UseCentralRoutePrefix(this RazorPagesOptions opts, IRouteTemplateProvider routeAttribute)
		{
			opts.Conventions.Insert(0, new PageConvention(routeAttribute));
		}*/
	}
}
