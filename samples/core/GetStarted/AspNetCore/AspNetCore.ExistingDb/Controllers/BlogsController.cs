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

		public IActionResult Create()
		{
			return View();
		}

		[HttpPost("Blogs/ItemAction/{id}/{ajax}")]
		[HttpDelete("Blogs/ItemAction/{id}/{ajax}")]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> ItemAction(int id, bool ajax, string url, string action = "")
		{
			/*if (!ModelState.IsValid)
			{
				if (ajax)
					return new JsonResult("error");
				else
				{
					return View("Index", null);
				}
			}*/

			ActionResult result;
			switch (action?.ToLower())
			{
				case "edit":
					result = await Edit(id, url, ajax);
					break;
				case "delete":
					result = await Delete(id, ajax);
					break;
				default:
					throw new NotSupportedException($"Unknown {nameof(action)} {action}");
			}
			if (ajax)
			{
				return result;
			}
			else
			{
				return RedirectToAction("Index");
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

				return /*Ok(*/Json("deleted")/*)*/;
			}
			else
				return NotFound();
		}

		[HttpPost]
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
