#if DEBUG
using Abiosoft.DotNet.DevReload;
#endif
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

#if DEBUG
    public sealed class MyDevReloadOptions : DevReloadOptions
	{
		public MyDevReloadOptions(string appRootPath)
		{
            DevReloadPath = appRootPath + "js/__DevReload";
            Directory = "./";
			IgnoredSubDirectories = new string[] { ".git", ".node_modules", "bin", "obj" };
			StaticFileExtensions = new string[] { "css", "js", "html", "cshtml" };
			MaxConnectionFailedCount = 20;
			CheckIntervalDelay = 2000;
			PopoutHtmlTemplate = @"<div id='reload' class='toast' role='alert' aria-live='assertive' aria-atomic='true'
	data-bs-autohide='false' data-bs-animation='true' style='position:absolute; top:0; right:0; z-index:9999; display:none'>
  <div class='toast-header'>
    <svg class='rounded me-2' width='20' height='20' xmlns='http://www.w3.org/2000/svg' preserveAspectRatio='xMidYMid slice' focusable='false' role='img'><rect width='100%' height='100%' fill='red'></rect></svg>
    <strong class='me-auto'>DevReload</strong>
    <small>just now</small>
    <button type='button' class='btn-close' data-bs-dismiss='toast' aria-label='Close'></button>
  </div>
  <div class='toast-body'>
    DevReload - Reloading page...
  </div>
</div>
<script>
	$('#reload').toast('hide');
</script>";
			TemplateActivationJSFragment = @"$('#reload').show().toast('show');";
            UseSignalR = true;
            SignalRClientSide = @$"<script src='{appRootPath}lib/signalr/browser/signalr.min.js'></script>";
                //<script src='{appRootPath}lib/msgpack5/msgpack5.min.js'></script>
                //<script src='{appRootPath}lib/signalr-protocol-msgpack/browser/signalr-protocol-msgpack.min.js'></script>";
            SignalRHubPath = appRootPath + "DevReloadSignalR";
        }
    }
#endif

}
