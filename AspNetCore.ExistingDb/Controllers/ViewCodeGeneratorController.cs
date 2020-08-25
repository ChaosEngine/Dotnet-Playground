using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language;
using System.IO;

namespace EFGetStarted.AspNetCore.ExistingDb.Controllers
{
	[Route("[controller]")]
	public sealed class ViewCodeGeneratorController : Controller
	{
		private string GetCompiledViewCode()
		{
#if DEBUG
			if (System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), @"Views\ViewCodeGenerator\Index.cshtml")))
			{
				var projectEngine = RazorProjectEngine.Create(
					RazorConfiguration.Default,
					RazorProjectFileSystem.Create(Directory.GetCurrentDirectory()));
				var item = projectEngine.FileSystem.GetItem(@"\Views\ViewCodeGenerator\Index.cshtml", FileKinds.Legacy);
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
