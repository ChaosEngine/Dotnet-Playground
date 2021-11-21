using DotnetPlayground.Models;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DotnetPlayground.Web.Helpers
{
	[JsonSerializable(typeof(Dictionary<string, string>))]
	public partial class DictStringString_Context : JsonSerializerContext
	{
	}


	[JsonSerializable(typeof(DateTime))]
	public partial class DateTime_Context : JsonSerializerContext
	{
	}

	[JsonSerializable(typeof(RandomData))]
	public partial class RandomData_Context : JsonSerializerContext
	{
	}


}
