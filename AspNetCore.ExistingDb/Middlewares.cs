using EFGetStarted.AspNetCore.ExistingDb.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading.Tasks;


namespace EFGetStarted.AspNetCore.ExistingDb
{
	public static class MiddlewareExtensions
	{
		public static IApplicationBuilder UseEnvironmentTitleDisplay(this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<EnvironmentTitleDisplay>();
		}
	}

	internal class EnvironmentTitleDisplay
	{
		private readonly IHostingEnvironment _env;
		private readonly RequestDelegate _next;

		public EnvironmentTitleDisplay(RequestDelegate next, IHostingEnvironment env)
		{
			_next = next;
			_env = env;
		}

		public async Task Invoke(HttpContext context)
		{
			var existingBody = context.Response.Body;

			using (var newBody = new MemoryStream())
			{
				context.Response.Body = newBody;

				await _next(context);

				context.Response.Body = existingBody;

				// Don't do anything if the type isn't HTML
				if (!context.Response.ContentType.StartsWith("text/html"))
				{
					await context.Response.WriteAsync(new StreamReader(newBody).ReadToEnd());
					return;
				}
				newBody.Seek(0, SeekOrigin.Begin);

				var newContent = new StreamReader(newBody).ReadToEnd();
				newContent = newContent.Replace("</title>", $" {_env.EnvironmentName}</title>");

				await context.Response.WriteAsync(newContent);
			}
		}
	}
#if DEBUG
	public class CustomCompilationService : DefaultRoslynCompilationService, ICompilationService
	{
		public CustomCompilationService(CSharpCompiler compiler,
			IOptions<RazorViewEngineOptions> optionsAccessor,
			IRazorViewEngineFileProviderAccessor fileProviderAccessor,
			ILoggerFactory loggerFactory)
			: base(compiler, fileProviderAccessor, optionsAccessor, loggerFactory)
		{

		}

		CompilationResult ICompilationService.Compile(RelativeFileInfo fileInfo, string compilationContent)
		{
			if (fileInfo.RelativePath == "/Views/ViewCodeGenerator/Index.cshtml")
			{
				ViewCodeGeneratorController.CompiledViewCode = compilationContent;
			}
			return base.Compile(fileInfo, compilationContent);
		}
	}
#endif
}
