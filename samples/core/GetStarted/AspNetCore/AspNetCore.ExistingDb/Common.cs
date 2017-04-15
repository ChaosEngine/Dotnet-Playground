using AspNetCore.ExistingDb.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EFGetStarted.AspNetCore.ExistingDb
{
	public static class ConfigurrationExtensions
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
			session.SetString(key, JsonConvert.SerializeObject(value));
		}

		public static T Get<T>(this ISession session, string key)
		{
			var value = session.GetString(key);

			return value == null ?
				default(T) :
				JsonConvert.DeserializeObject<T>(value);
		}
	}

	public static class TempDataExtensions
	{
		public static void Put<T>(this ITempDataDictionary tempData, string key, T value) where T : class
		{
			tempData[key] = JsonConvert.SerializeObject(value);
		}

		public static T Get<T>(this ITempDataDictionary tempData, string key) where T : class
		{
			object value;
			tempData.TryGetValue(key, out value);
			return value == null ?
				default(T) :
				JsonConvert.DeserializeObject<T>((string)value);
		}
	}
}
