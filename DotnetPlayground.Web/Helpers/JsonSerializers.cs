using DotnetPlayground.Models;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DotnetPlayground.Web.Helpers
{
    /// Home Page


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


    /// Blogs

    [JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(List<Post>))]
    public partial class ListPost_Context : JsonSerializerContext { }

    [JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(Post))]
    public partial class Post_Context : JsonSerializerContext { }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Blog))]
    public partial class Blog_Context : JsonSerializerContext { }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(DecoratedBlog))]
    public partial class DecoratedBlog_Context : JsonSerializerContext { }

    [JsonSerializable(typeof(string))]
    public partial class String_Context : JsonSerializerContext { }

    /// CSP

    [JsonSerializable(typeof(CspReportRequest))]
    public partial class CspReportRequest_Context : JsonSerializerContext { }


    /// Hashes

    [JsonSerializable(typeof(HashInput))]
    public partial class HashInput_Context : JsonSerializerContext { }

    [JsonSerializable(typeof(Hashes))]
    public partial class Hashes_Context : JsonSerializerContext { }

    [JsonSerializable(typeof(ThinHashes))]
    public partial class ThinHashes_Context : JsonSerializerContext { }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(List<ThinHashes>))]
    public partial class IEnumerableThinHashes_Context : JsonSerializerContext { }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(HashesLoadResult))]
    public partial class HashesLoadResult_Context : JsonSerializerContext { }


    // WebcamGallery

    [JsonSerializable(typeof(AnnualTimelapseBag))]
    public partial class AnnualTimelapseBag_Context : JsonSerializerContext { }

}
