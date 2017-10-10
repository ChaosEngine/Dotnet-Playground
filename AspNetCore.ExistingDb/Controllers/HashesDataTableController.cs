using AspNetCore.ExistingDb.Repositories;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace EFGetStarted.AspNetCore.ExistingDb.Controllers
{
	public interface IHashesDataTableController : IDisposable
	{
		IActionResult Index();
		Task<IActionResult> Load(string sort, string order, string search, int limit, int offset, string extraParam);
	}

	public class HashesDataTableController : BaseController<ThinHashes>, IHashesDataTableController
	{
		private readonly IHostingEnvironment _hostingEnvironment;
		private readonly ILogger<BaseController<ThinHashes>> _logger;
		private readonly IHashesRepository _repo;
		private static readonly JsonSerializerSettings _serializationSettings =
			new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore, Formatting = Formatting.None };

		#region Old code
		/*private IQueryable<ThinHashes> BaseItems
		{
			get
			{
				IQueryable<ThinHashes> result = _repo.GetAll()
					//.Take(2000)
					;
				return result;
			}
		}*/
		#endregion Old code

		public HashesDataTableController(IHostingEnvironment env, IHashesRepository repo,
			ILogger<BaseController<ThinHashes>> logger) : base()
		{
			_hostingEnvironment = env;
			_logger = logger;
			_repo = repo;
			_repo.SetReadOnly(true);
		}

		[HttpGet("HashesDataTable")]
		public IActionResult Index()
		{
			#region Old code
			//String path = Request.Path;

			//var controllerName = GetType().Name.Substring(0, GetType().Name.IndexOf("controller", StringComparison.CurrentCultureIgnoreCase));
			//if (path == "/")
			//	path += controllerName;

			//if (path.IndexOf(@"/index", StringComparison.OrdinalIgnoreCase) == -1)
			//	return Redirect((path + "/Index").Replace("//", "/"));
			#endregion Old code

			string view_name = "Views/Hashes/BootstrapDataTable.cshtml";
			return View(view_name);
		}

		[HttpGet]
		public async Task<IActionResult> Load(string sort, string order, string search, int limit, int offset, string extraParam)
		{
			#region Old code
			// Get entity fieldnames
			/*IEnumerable<string> columnNames = AllColumnNames;

			// Create a seperate list for searchable field names   
			//IEnumerable<string> searchFields = new List<string>(columnNames);
			// Exclude field Iso2 for filtering 
			//searchFields.Remove("ISO2");



			// Perform filtering
			IQueryable<ThinHashes> items = SearchItems(BaseItems, search, columnNames);

			// Sort the filtered items and apply paging
			var found = ItemsToJson(items, columnNames, sort, order, limit, offset);*/
			#endregion Old code

			CancellationToken token = HttpContext.RequestAborted;
			try
			{
				//await Task.Delay(2_000, token);

				var found = await _repo.SearchAsync(sort, order, search, offset, limit, token);

				var result = new
				{
					total = found.Count,
					rows = found.Itemz
				};

				return Json(result, _serializationSettings);
			}
			catch (OperationCanceledException ex)
			{
				_logger.LogWarning(ex, $"!!!!!!!!!!!!!!!Cancelled {nameof(Load)}::{nameof(_repo.SearchAsync)}({sort}, {order}, {search}, {offset}, {limit}, {token})");
				return Ok();
			}
			catch (Exception)
			{
				throw;
			}
		}
	}
}
