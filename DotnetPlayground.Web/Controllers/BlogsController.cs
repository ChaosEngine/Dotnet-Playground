using DotnetPlayground.Repositories;
using DotnetPlayground.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetPlayground.Controllers
{
	/// <summary>
	/// For testing
	/// </summary>
	public interface IBlogsController : IDisposable
	{
		IActionResult Create();
		Task<ActionResult> Create(Blog blog);
		Task<IActionResult> Index();
		Task<ActionResult> BlogAction(DecoratedBlog blog, bool ajax, BlogActionEnum action = BlogActionEnum.Unknown);
	}

	[Route("[controller]")]
	public class BlogsController : Controller, IBlogsController
	{
		public const string ASPX = "Blogs";

		private readonly ILogger<BlogsController> _logger;
		private readonly IConfiguration _configuration;
		private readonly IDataProtector _protector;
		private readonly IBloggingRepository _repo;

		public BlogsController(IBloggingRepository repo, ILogger<BlogsController> logger, IConfiguration configuration, IDataProtectionProvider protectionProvider)
		{
			_repo = repo;
			_logger = logger;
			_configuration = configuration;
			_protector = protectionProvider.CreateProtector(typeof(DecoratedBlog).FullName);
		}

		private async Task<IEnumerable<DecoratedBlog>> GetBlogs()
		{
			var lst = (await _repo.GetAllAsync(nameof(DecoratedBlog.Post)));

			return lst.Select(b => new DecoratedBlog(b, _protector));
		}

		public async Task<IActionResult> Index()
		{
			var lst = await GetBlogs();

			return View(lst);
		}

		[HttpGet("[action]")]
		public IActionResult Create()
		{
			return View();
		}

		[Route(@"{operation:regex(^(" + nameof(BlogActionEnum.Delete) + "|" + nameof(BlogActionEnum.Edit) + ")$)}/{" +
			nameof(DecoratedBlog.BlogId) + "}/{ajax}")]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> BlogAction(DecoratedBlog blog, bool ajax, BlogActionEnum operation = BlogActionEnum.Unknown)
		{
			if (operation == BlogActionEnum.Delete)
				ModelState.Remove(nameof(blog.Url));

			if (!ModelState.IsValid)
			{
				if (ajax)
					return Json("error");
				else
				{
					IEnumerable<DecoratedBlog> lst = await GetBlogs();
					lst = lst.Where(l => l.ProtectedID != blog.ProtectedID).Union(new[] { blog });
					return View(nameof(Index), lst);
				}
			}

			ActionResult result;
			switch (operation)
			{
				case BlogActionEnum.Edit:
					result = await EditBlog(blog.BlogId, blog.Url, ajax);
					break;
				case BlogActionEnum.Delete:
					result = await DeleteBlog(blog.BlogId, ajax);
					break;
				case BlogActionEnum.Unknown:
				default:
					throw new NotSupportedException($"Unknown {nameof(operation)} {operation}");
			}
			if (ajax)
				return result;
			else
			{
				var appRootPath = _configuration.AppRootPath();
				var destination_url = appRootPath + ASPX;
				return Redirect(destination_url);
			}
		}

		[Route(@"{operation:regex(^(" + nameof(PostActionEnum.GetPosts) + ")$)}/{" + nameof(DecoratedBlog.BlogId) + "}")]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> GetPosts(int blogId)
		{
			var lst = await _repo.GetPostsFromBlogAsync(blogId);

			return Json(lst, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });
		}

		[Route(@"{operation:regex(^(" +
			nameof(PostActionEnum.DeletePost) + "|" +
			nameof(PostActionEnum.EditPost) + "|" +
			nameof(PostActionEnum.AddPost) +
			")$)}/{" + nameof(DecoratedBlog.BlogId) + "}/{ajax}")]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> PostAction(int blogId, bool ajax, Post post, PostActionEnum operation = PostActionEnum.Unknown)
		{
			if (operation == PostActionEnum.DeletePost)
			{
				ModelState.Remove(nameof(post.Content));
				ModelState.Remove(nameof(post.Title));
			}

			if (!ModelState.IsValid)
			{
				if (ajax)
					return Json("error");
				else
				{
					IEnumerable<DecoratedBlog> lst = await GetBlogs();
					return View(nameof(Index), lst);
				}
			}

			ActionResult result;
			switch (operation)
			{
				case PostActionEnum.EditPost:
					result = await EditPost(post.BlogId, post, ajax);
					break;
				case PostActionEnum.DeletePost:
					result = await DeletePost(post.BlogId, post.PostId, ajax);
					break;
				case PostActionEnum.AddPost:
					result = await AddPost(post.BlogId, post, ajax);
					break;
				case PostActionEnum.Unknown:
				default:
					throw new NotSupportedException($"Unknown {nameof(operation)} {operation}");
			}
			if (ajax)
				return result;
			else
			{
				var appRootPath = _configuration.AppRootPath();
				var destination_url = appRootPath + ASPX;
				return Redirect(destination_url);
			}
		}

		protected async Task<ActionResult> AddPost(int blogId, Post post, bool ajax)
		{
			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(0, $"id = {blogId} title = {(post.Title ?? "<null>")} {nameof(post.Content)} = {post.Content ?? "<null>"}");
			});

			if (blogId <= 0 || string.IsNullOrEmpty(post.Title) || string.IsNullOrEmpty(post.Content)) return BadRequest(ModelState);

			Blog blog = await _repo.GetSingleAsync(blogId);
			if (blog != null)
			{
				blog.Post.Add(post);
				int modified = await _repo.SaveAsync();

				return Json(new Post
				{
					PostId = post.PostId,
					BlogId = post.BlogId,
					Title = post.Title,
					Content = post.Content
				}, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });
			}

			return NotFound();
		}

		protected async Task<ActionResult> DeletePost(int blogId, int postId, bool ajax)
		{
			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(2, $"blogId = {blogId}, {nameof(postId)} = {postId}");
			});

			if (blogId <= 0 || postId <= 0) return BadRequest(ModelState);

			var deleted = await _repo.DeletePostAsync(blogId, postId);
			if (deleted)
			{
				await _repo.SaveAsync();

				return Json("deleted post");
			}
			else
				return NotFound();
		}

		protected async Task<ActionResult> EditPost(int blogId, Post post, bool ajax)
		{
			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(0, $"id = {blogId} title = {(post.Title ?? "<null>")} {nameof(post.Content)} = {post.Content ?? "<null>"}");
			});

			if (blogId <= 0 || string.IsNullOrEmpty(post.Title) || string.IsNullOrEmpty(post.Content)) return BadRequest(ModelState);

			var posts = await _repo.GetPostsFromBlogAsync(blogId);
			Post db_post = posts.FirstOrDefault(x => x.PostId == post.PostId);
			if (db_post != null)
			{
				db_post.Title = post.Title;
				db_post.Content = post.Content;
				int modified = await _repo.SaveAsync();

				return Json(new Post
				{
					PostId = post.PostId,
					BlogId = post.BlogId,
					Title = post.Title,
					Content = post.Content
				}, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });
			}

			return NotFound();
		}

		//[HttpPost("Blogs/Edit/{id:int}/{ajax:bool}")]
		//[ValidateAntiForgeryToken]
		protected async Task<ActionResult> EditBlog(int blogId, string url, bool ajax)
		{
			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(0, $"id = {blogId} url = {(url ?? "<null>")} {nameof(ajax)} = {ajax.ToString()}");
			});

			if (blogId <= 0 || string.IsNullOrEmpty(url)) return BadRequest(ModelState);

			Task<Blog> tsk = _repo.GetSingleAsync(blogId);
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
		protected async Task<ActionResult> DeleteBlog(int blogId, bool ajax)
		{
			var logger_tsk = Task.Run(() =>
			{
				_logger.LogInformation(2, $"id = {blogId}, {nameof(ajax)} = {ajax.ToString()}");
			});

			if (blogId <= 0) return BadRequest(ModelState);

			Task<Blog> tsk = _repo.GetSingleAsync(blogId);
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

		[HttpPost("[action]")]
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

				var route = appRootPath + ASPX;
				return Redirect(route);
			}

			return View(blog);
		}
	}
}
