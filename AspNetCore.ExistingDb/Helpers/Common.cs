#if DEBUG
using Abiosoft.DotNet.DevReload;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EFGetStarted.AspNetCore.ExistingDb
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
		public static void Set<T>(this ISession session, string key, T value)
		{
			session.SetString(key, JsonSerializer.Serialize(value));
		}

		public static T Get<T>(this ISession session, string key)
		{
			var value = session.GetString(key);

			return value == null ?
				default(T) :
				JsonSerializer.Deserialize<T>(value);
		}
	}

	public static class TempDataExtensions
	{
		public static void Put<T>(this ITempDataDictionary tempData, string key, T value) where T : class
		{
			tempData[key] = JsonSerializer.Serialize(value);
		}

		public static T Get<T>(this ITempDataDictionary tempData, string key) where T : class
		{
			object value;
			tempData.TryGetValue(key, out value);
			return value == null ?
				default(T) :
				JsonSerializer.Deserialize<T>((string)value);
		}
	}

	public sealed class DBConfigShower
	{
		public string DBConfig { get; set; }

		public DBConfigShower()
		{
			//DBConfig = "<null>";
		}
	}

#if DEBUG
	public sealed class MyDevReloadOptions : DevReloadOptions
	{
		public MyDevReloadOptions()
		{
			Directory = "./";
			IgnoredSubDirectories = new string[] { ".git", ".node_modules", "bin", "obj" };
			StaticFileExtensions = new string[] { "css", "js", "html", "cshtml" };
			MaxConnectionFailedCount = 20;
			CheckIntervalDelay = 2000;
			PopoutHtmlTemplate = @"<div id='reload' class='toast' role='alert' aria-live='assertive' aria-atomic='true'
	data-autohide='false' data-animation='true' style='position: absolute; top: 0; right: 0; z-index: 9999'>
  <div class='toast-header'>
    <svg class='bd-placeholder-img rounded mr-2' width='20' height='20' xmlns='http://www.w3.org/2000/svg' preserveAspectRatio='xMidYMid slice' focusable='false' role='img'><rect width = '100%' height='100%' fill='red'></rect></svg>
    <strong class='mr-auto'>DevReload</strong>
    <small>just now</small>
    <button type='button' class='ml-2 mb-1 close' data-dismiss='toast' aria-label='Close'>
      <span aria-hidden='true'>×</span>
    </button>
  </div>
  <div class='toast-body'>
    DevReload - Reloading page...
  </div>
</div>
<script>
	$('#reload').toast('hide');
</script>";
			TemplateActivationJSFragment = @"$('#reload').toast('show');";
		}
	}
#endif

}
