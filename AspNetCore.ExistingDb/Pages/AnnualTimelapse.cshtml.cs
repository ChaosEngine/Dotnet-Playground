using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.ExistingDb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace AspNetCore.ExistingDb.Models
{
	[Authorize(Roles = "Administrator")]
	public sealed class AnnualTimelapseModel : AnnualMovieListGeneratorModel
	{
		public sealed class SomeBag
		{
			public string Result { get; set; }

			public IEnumerable<object[]> Product { get; set; }
		}

		public const string ASPX = "AnnualTimelapse";

		public AnnualTimelapseModel(IConfiguration configuration) : base(configuration)
		{
		}

		public async Task<IActionResult> OnPostSecretAction([FromServices] IServiceProvider services, SomeBag bag)
		{
			if (!base.IsAnnualMovieListAvailable(checkFileExistance: true))
				return await Task.FromResult<IActionResult>(base.Forbid());

			if (services == null)
				return await Task.FromResult<IActionResult>(new JsonResult(new SomeBag { Result = "Error0" }));


			var operation = new YouTubePlaylistDumpOperation();
			await operation.DoWorkAsync(services, Request.HttpContext.RequestAborted);
			var product = operation.Product;
			// var product = new List<object[]>();
			// product.Add(new object[]{"aaa","bbbb","ccccc","dddd"});

			var result = new SomeBag { Result = "Ok", Product = product };

			return await Task.FromResult<IActionResult>(new JsonResult(result));
		}
	}
}
