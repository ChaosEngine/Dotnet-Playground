/*
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DotnetPlayground;

public sealed class RequestDecompressionMiddleware
{
    private readonly RequestDelegate _next;

    public RequestDecompressionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Method == "POST" && context.Request.Headers.ContainsKey("Content-Encoding"))
        {
            var encoding = context.Request.Headers["Content-Encoding"].ToString().ToLowerInvariant();

            if (encoding == "gzip" || encoding == "deflate")
            {
                context.Request.EnableBuffering();

                using (Stream decompressionStream = encoding == "gzip"
                    ? new GZipStream(context.Request.Body, CompressionMode.Decompress)
                    : new DeflateStream(context.Request.Body, CompressionMode.Decompress))
                {
                    var decompressedBodyStream = new MemoryStream();
                    await decompressionStream.CopyToAsync(decompressedBodyStream);
                    decompressedBodyStream.Seek(0, SeekOrigin.Begin);
                    context.Request.Body = decompressedBodyStream;
                }
                await _next(context);
            }
        }
        else
        {
            await _next(context);
        }
    }
}
*/