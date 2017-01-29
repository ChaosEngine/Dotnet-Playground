using EFGetStarted.AspNetCore.ExistingDb.Models;
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
		private BloggingContext _context;
		private ILogger<BlogsController> _logger;
		private IConfiguration _configuration;

		public BlogsController(BloggingContext context, ILogger<BlogsController> logger, IConfiguration configuration)
		{
			_context = context;
			_logger = logger;
			_configuration = configuration;
		}

		private IAsyncEnumerable<Blog> GetBlogs()
		{
			var lst = _context.Blog.ToAsyncEnumerable();

			return lst;
		}

		public async Task<IActionResult> Index()
		{
			var lst = await (GetBlogs().ToList());

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
		public async Task<ActionResult> ItemAction(Blog blog, bool ajax, BlogActionEnum action = BlogActionEnum.Unknown)
		{
			if (action == BlogActionEnum.Delete)
				ModelState.Remove(nameof(blog.Url));

			if (!ModelState.IsValid)
			{
				if (ajax)
					return new JsonResult("error");
				else
				{
					IEnumerable<Blog> lst = await (GetBlogs().ToList());
					lst = lst.Where(x => x.BlogId != blog.BlogId).Union(new[] { blog });
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
				var appRootPath = _configuration["AppRootPath"];
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

			Task<Blog> tsk = _context.Blog.FindAsync(id);
			Blog blog = await tsk;
			if (blog != null && url != blog.Url)
			{
				blog.Url = url;
				await _context.SaveChangesAsync();

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

			Task<Blog> tsk = _context.Blog.FindAsync(id);
			Blog blog = await tsk;
			if (blog != null)
			{
				_context.Remove(blog);
				await _context.SaveChangesAsync();

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
				var appRootPath = _configuration["AppRootPath"];

				await _context.Blog.AddAsync(blog);
				await _context.SaveChangesAsync();

				var route = appRootPath + "Blogs";
				return Redirect(route);
			}

			return View(blog);
		}
	}
}
