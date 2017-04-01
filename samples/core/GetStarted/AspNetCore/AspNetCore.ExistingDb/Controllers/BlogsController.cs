using AspNetCore.ExistingDb.Repositories;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EFGetStarted.AspNetCore.ExistingDb.Controllers
{
	[Route("[controller]")]
	public class BlogsController : Controller
	{
		private readonly ILogger<BlogsController> _logger;
		private readonly IConfiguration _configuration;
		private readonly IDataProtector _protector;
		private readonly IBloggingRepository _repo;

		public BlogsController(IBloggingRepository repo, ILogger<BlogsController> logger, IConfiguration configuration, IDataProtectionProvider protectionProvider)
		{
			_repo = repo;
			_logger = logger;
			_configuration = configuration;
			_protector = protectionProvider.CreateProtector(GetType().FullName);
		}

		private async Task<IEnumerable<DecoratedBlog>> GetBlogs()
		{
			var lst = (await _repo.GetAllAsync());

			return lst.Select(b => new DecoratedBlog(b, _protector.Protect(b.BlogId.ToString())));
		}

		public async Task<IActionResult> Index()
		{
			var lst = await GetBlogs();

			return View(lst);
		}

		[HttpGet(nameof(Create))]
		public IActionResult Create()
		{
			return View();
		}

		//[HttpPost("Blogs/Edit/{BlogId}/{ajax}")]
		//[HttpPost("Blogs/Delete/{BlogId}/{ajax}")]
		//[HttpDelete("Blogs/Delete/{BlogId}/{ajax}")]
		[Route(@"{x:regex(^(" + nameof(Delete) + "|" + nameof(Edit) + ")$)}" + @"/{BlogId}/{ajax}")]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> ItemAction(DecoratedBlog blog, bool ajax, BlogActionEnum action = BlogActionEnum.Unknown)
		{
			if (action == BlogActionEnum.Delete)
				ModelState.Remove(nameof(blog.Url));

			if (!ModelState.IsValid)
			{
				if (ajax)
					return Json("error");
				else
				{
					IEnumerable<DecoratedBlog> lst = await GetBlogs();
					lst = lst.Where(x => x.ProtectedID != blog.ProtectedID).Union(new[] { blog });
					return View(nameof(Index), lst);
				}
			}

			ActionResult result;
			switch (action)
			{
				case BlogActionEnum.Edit:
					result = await Edit(blog.BlogId, blog.Url, ajax);
					break;
				case BlogActionEnum.Delete:
					result = await Delete(blog.BlogId, ajax);
					break;
				case BlogActionEnum.Unknown:
				default:
					throw new NotSupportedException($"Unknown {nameof(action)} {action.ToString()}");
			}
			if (ajax)
				return result;
			else
			{
				var appRootPath = _configuration.AppRootPath();
				var destination_url = appRootPath + "Blogs";
				return Redirect(destination_url);
			}
		}

		//[HttpPost("Blogs/Edit/{id:int}/{ajax:bool}")]
		//[ValidateAntiForgeryToken]
		protected async Task<ActionResult> Edit(int id, string url, bool ajax)
		{
			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(0, $"id = {id} url = {(url ?? "<null>")} {nameof(ajax)} = {ajax.ToString()}");
			});

			if (id <= 0 || string.IsNullOrEmpty(url)) return BadRequest(ModelState);

			Task<Blog> tsk = _repo.GetSingleAsync(id);
			Blog blog = await tsk;
			if (blog != null && url != blog.Url)
			{
				blog.Url = url;
				_repo.Edit(blog);
				int modified = await _repo.SaveAsync();

				return Json(blog);
			}

			return NotFound();
		}

		//[HttpPost("Blogs/Delete/{id:int}/{ajax:bool}")]
		//[ValidateAntiForgeryToken]
		protected async Task<ActionResult> Delete(int id, bool ajax)
		{
			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(2, $"id = {id}, {nameof(ajax)} = {ajax.ToString()}");
			});

			if (id <= 0) return BadRequest(ModelState);

			Task<Blog> tsk = _repo.GetSingleAsync(id);
			Blog blog = await tsk;
			if (blog != null)
			{
				_repo.Delete(blog);
				await _repo.SaveAsync();

				return Json("deleted");
			}
			else
				return NotFound();
		}

		[HttpPost(nameof(Create))]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(Blog blog)
		{
			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(1, $"id = {blog.BlogId} url = {(blog.Url ?? "<null>")}");
			});

			if (ModelState.IsValid)
			{
				var appRootPath = _configuration.AppRootPath();

				await _repo.AddAsync(blog);
				await _repo.SaveAsync();

				var route = appRootPath + "Blogs";
				return Redirect(route);
			}

			return View(blog);
		}
	}
}
