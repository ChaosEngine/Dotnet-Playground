using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetPlayground.Services;
using DotnetPlayground.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace DotnetPlayground.Models
{
	[Authorize(Roles = "Administrator")]
	public sealed class AnnualTimelapseModel : AnnualMovieListGeneratorModel
	{
		public const string ASPX = "AnnualTimelapse";

		public AnnualTimelapseModel(IConfiguration configuration) : base(configuration)
		{
		}

		public async Task<IActionResult> OnPostSecretAction([FromServices] IServiceProvider services, AnnualTimelapseBag bag)
		{
			if (!base.IsAnnualMovieListAvailable(checkFileExistance: true))
				return await Task.FromResult<IActionResult>(base.Forbid());

			if (services == null)
				return await Task.FromResult<IActionResult>(new JsonResult(
					new AnnualTimelapseBag { Result = "Error0" }, AnnualTimelapseBag_Context.Default.Options)
				);


			var operation = new YouTubePlaylistDumpOperation();
			await operation.DoWorkAsync(services, Request.HttpContext.RequestAborted);
			var product = operation.Product;
			//var product = new List<object[]>
			//{
			//	new object[] { "aaa", "bbbb", "ccccc", "dddd" }
			//};

			var result = new AnnualTimelapseBag { Result = "Ok", Product = product };

			return await Task.FromResult<IActionResult>(new JsonResult(result, AnnualTimelapseBag_Context.Default.Options));
		}
	}
}
