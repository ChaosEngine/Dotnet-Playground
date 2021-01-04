using Microsoft.AspNetCore.Mvc;
using System.IO;
#if DEBUG
using Microsoft.AspNetCore.Razor.Language;
#endif

namespace DotnetPlayground.Controllers
{
	[Route("[controller]")]
	public sealed class ViewCodeGeneratorController : Controller
	{
		private string GetCompiledViewCode()
		{
#if DEBUG
			string view = @"Pages\WebCamGallery.cshtml";
			if (System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), view)))
			{
				var projectEngine = RazorProjectEngine.Create(
					RazorConfiguration.Default,
					RazorProjectFileSystem.Create(Directory.GetCurrentDirectory()));
				var item = projectEngine.FileSystem.GetItem('\\' + view, FileKinds.Legacy);
				var output = projectEngine.Process(item);

				// Things available
				var syntaxTree = output.GetSyntaxTree();
				var intermediateDocument = output.GetDocumentIntermediateNode();
				var csharpDocument = output.GetCSharpDocument();

				return csharpDocument.GeneratedCode;
			}
			else
#endif
				return "";
		}

		public IActionResult Index()
		{
			string compiledViewCode = GetCompiledViewCode();

			ViewData["CompiledViewCode"] = compiledViewCode;

			return View();
		}
	}
}
