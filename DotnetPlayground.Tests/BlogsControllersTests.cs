using DotnetPlayground.Repositories;
using DotnetPlayground.Tests;
using DotnetPlayground.Controllers;
using DotnetPlayground.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System;

namespace Controllers
{
	public class BlogsControllers : BaseControllerTest
	{
		private static Moq.Mock<IBloggingRepository> MockBloggingRepository()
		{
			var mock = new Moq.Mock<IBloggingRepository>();
			var lst = new List<Blog>(3)
			{
				new Blog
				{
					BlogId = 1,
					Post = new List<Post>(new []
					{
						new Post
						{
							BlogId = 1,
							Content = "content",
							PostId = 1,
							Title = "title"
						}
					}),
					Url = "http://www.some-url.com"
				}
			};
			Blog to_add = null;


			mock.Setup(r => r.GetAllAsync()).Returns(() =>
			{
				return Task.FromResult(lst.ToList());
			});

			mock.Setup(r => r.GetAllAsync(Moq.It.IsAny<string>())).Returns<string>((s) =>
			{
				return Task.FromResult(lst.ToList());
			});

			mock.Setup(r => r.AddAsync(Moq.It.IsAny<Blog>())).Returns<Blog>((s) =>
			{
				to_add = s;
				return Task.FromResult(to_add);
			});

			mock.Setup(r => r.SaveAsync()).Returns(() =>
			{
				if (to_add != null)
				{
					lst.Add(to_add);
					to_add = null;
					return Task.FromResult(1);
				}
				return Task.FromResult(0);
			});

			mock.Setup(r => r.GetSingleAsync(Moq.It.IsAny<object[]>())).Returns<object[]>((s) =>
			{
				Blog found = lst.FirstOrDefault(_ => _.BlogId == (int)s.FirstOrDefault());
				return Task.FromResult(found);
			});

			//mock.Setup(r => r.GetSingleAsync(Moq.It.IsNotIn(new object[] { 1, 2 }))).Returns(() =>
			//{
			//	return Task.FromResult<Blog>(null);
			//});

			mock.Setup(r => r.Edit(Moq.It.IsAny<Blog>())).Callback<Blog>((s) =>
			{
				var found = lst.FirstOrDefault(_ => _.BlogId == s.BlogId);
				found.Url = s.Url;
			});

			mock.Setup(r => r.Delete(Moq.It.IsAny<Blog>())).Callback<Blog>((s) =>
			{
				var found = lst.FirstOrDefault(_ => _.BlogId == s.BlogId);
				if (found != null)
				{
					lst.Remove(found);
				}
			});

			mock.Setup(r => r.DeletePostAsync(Moq.It.IsAny<int>(), Moq.It.IsAny<int>())).Returns<int, int>((blogId, postId) =>
			{
				var blog = lst.FirstOrDefault(b => b.BlogId == blogId && b.Post.Any(p => p.PostId == postId));
				if (blog != null)
				{
					var post = blog.Post.FirstOrDefault(_ => _.PostId == postId);
					blog.Post.Remove(post);
					return Task.FromResult(true);
				}
				return Task.FromResult(false);
			});

			mock.Setup(r => r.GetPostsFromBlogAsync(Moq.It.IsAny<int>())).Returns<int>((blogId) =>
			{
				var posts = lst.FirstOrDefault(p => p.BlogId == blogId)?.Post?.ToList();
				return Task.FromResult(posts);
			});

			return mock;
		}

		public BlogsControllers() : base()
		{
			SetupServices();
		}

		[Fact]
		public async Task Index_GetAll()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				// Act
				var result = await controller.Index();

				// Assert
				Assert.NotNull(result);

				Assert.IsType<ViewResult>(result);
				Assert.IsAssignableFrom<IEnumerable<DecoratedBlog>>(((ViewResult)result).Model);
				Assert.NotEmpty(((IEnumerable<DecoratedBlog>)((ViewResult)result).Model));
				Assert.Equal(1, ((IEnumerable<DecoratedBlog>)((ViewResult)result).Model).First().BlogId);
				Assert.Equal("http://www.some-url.com", ((IEnumerable<DecoratedBlog>)((ViewResult)result).Model).First().Url);
			}
		}

		[Fact]
		public async Task Create()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				// Act
				var result = controller.Create();

				// Assert
				Assert.NotNull(result);

				Assert.IsType<ViewResult>(result);
				Assert.Null(((ViewResult)result).Model);
			}

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				// Act
				var result = await controller.Create(new Blog
				{
					BlogId = 2,
					Post = null,
					Url = "http://www.internet.com"
				});

				// Assert
				Assert.NotNull(result);

				Assert.IsType<RedirectResult>(result);
				Assert.Equal($"{Configuration["AppRootPath"]}{BlogsController.ASPX}", ((RedirectResult)result).Url);
			}
		}

		[Fact]
		public async Task InvalidCreate()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				// Act
				((Controller)controller).ModelState.AddModelError(nameof(Blog.Url), "bad url");

				var model = new Blog
				{
					BlogId = 2,
					Post = null,
					Url = "bad_url_is_bad"
				};
				var result = await controller.Create(model);

				// Assert
				Assert.NotNull(result);

				Assert.IsNotType<RedirectResult>(result);
				Assert.IsType<ViewResult>(result);
				Assert.NotNull(((ViewResult)result).Model);
				Assert.Same(model, ((ViewResult)result).Model);
			}
		}

		[Fact]
		public async Task Edit()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();

			//1st add one object
			await repository.AddAsync(new Blog
			{
				BlogId = 2,
				Post = null,
				Url = "http://www.internet.com",
			});
			await repository.SaveAsync();

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				var model = new DecoratedBlog
				{
					BlogId = 2,
					Post = null,
					Url = "http://www.changed-internet.com",
					ProtectedID = "giberish"
				};

				// Act
				var result = await controller.BlogAction(model, true, BlogActionEnum.Edit);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<Blog>(((JsonResult)result).Value);
				Assert.Equal(2, ((Blog)((JsonResult)result).Value).BlogId);
				Assert.Contains("changed", ((Blog)((JsonResult)result).Value).Url);
			}
		}

		[Fact]
		public async Task InvalidEdit()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();

			//1st add one object
			await repository.AddAsync(new Blog
			{
				BlogId = -99999,
				Post = null,
				Url = "http://www.999999.com",
			});
			await repository.SaveAsync();

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				var model = new DecoratedBlog
				{
					BlogId = -99999,
					Post = null,
					Url = "http://www.minus999999.com",
					ProtectedID = "giberish"
				};

				// Act
				((Controller)controller).ModelState.AddModelError(nameof(Blog.Url), "bad id");

				var result = await controller.BlogAction(model, true, BlogActionEnum.Edit);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<string>(((JsonResult)result).Value);
				Assert.Equal("error", ((JsonResult)result).Value.ToString());
			}
		}

		[Fact]
		public async Task Delete()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();

			//1st add one object
			await repository.AddAsync(new Blog
			{
				BlogId = 2,
				Post = null,
				Url = "http://www.internet.com",
			});
			await repository.SaveAsync();

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				var model = new DecoratedBlog
				{
					BlogId = 2,
					Post = null,
					Url = "bad_url_but_no_matter",
					ProtectedID = "giberish"
				};

				// Act
				var result = await controller.BlogAction(model, true, BlogActionEnum.Delete);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<string>(((JsonResult)result).Value);
				Assert.Equal("deleted", ((JsonResult)result).Value.ToString());
			}
		}

		[Fact]
		public async Task InvalidDelete()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();

			//1st add one object
			await repository.AddAsync(new Blog
			{
				BlogId = 88888,
				Post = null,
				Url = "http://www.88888.com",
			});
			await repository.SaveAsync();

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				var model = new DecoratedBlog
				{
					BlogId = 99999,
					Post = null,
					Url = "bad_url_but_no_matter",
					ProtectedID = "giberish"
				};

				// Act
				var result = await controller.BlogAction(model, true, BlogActionEnum.Delete);

				// Assert
				Assert.IsType<NotFoundResult>(result);
				Assert.Equal(404, ((NotFoundResult)result).StatusCode);
			}
		}

		[Fact]
		public async Task Index_GetAllPosts()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				// Act
				var result = await controller.GetPosts(blogId: 1);

				// Assert
				Assert.NotNull(result);

				Assert.IsType<JsonResult>(result);
				Assert.IsAssignableFrom<IEnumerable<Post>>(((JsonResult)result).Value);
				Assert.NotEmpty(((IEnumerable<Post>)((JsonResult)result).Value));
				Assert.Equal(1, ((IEnumerable<Post>)((JsonResult)result).Value).First().BlogId);
				Assert.Equal("title", ((IEnumerable<Post>)((JsonResult)result).Value).First().Title);
				Assert.Equal("content", ((IEnumerable<Post>)((JsonResult)result).Value).First().Content);
			}
		}

		[Fact]
		public async Task CreatePost()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();
			var post = new Post
			{
				BlogId = 1,
				Title = "Ok, here's one...",
				Content = @"At a company that I used to work for, the CEO's brother  was  the
""system  operator"".It was his job to do backups, maintentance,
etc.Problem was, he didn't have a clue about Unix.  We were re-
quired to go through him to do anything, though."
			};
			ActionResult result;

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				// Act
				var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
				{
					//no real action - should throw
					result = await controller.PostAction(1, true, post);
				});
				Assert.Equal("operation 'Unknown' is unknown", exception.Message);


				// Act
				//proper operation
				result = await controller.PostAction(1, true, post, PostActionEnum.AddPost);

				// Assert
				Assert.NotNull(result);
				Assert.IsType<JsonResult>(result);
				Assert.Equal(post.BlogId, ((Post)((JsonResult)result).Value).BlogId);
				Assert.Equal(post.Title, ((Post)((JsonResult)result).Value).Title);
				Assert.Equal(post.Content, ((Post)((JsonResult)result).Value).Content);



				// Arrange
				//add one object
				await repository.AddAsync(new Blog
				{
					BlogId = 2,
					Post = new List<Post>(),
					Url = "http://www.internet.com",
				});
				await repository.SaveAsync();

				post = new Post
				{
					BlogId = 2,
					PostId = 1,
					Title = "title internet post",
					Content = "content internet post"
				};

				// Act
				result = await controller.PostAction(2, true, post, PostActionEnum.AddPost);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<Post>(((JsonResult)result).Value);
				Assert.Equal(2, ((Post)((JsonResult)result).Value).BlogId);
				Assert.Equal(1, ((Post)((JsonResult)result).Value).PostId);
				Assert.Equal(post.Title, ((Post)((JsonResult)result).Value).Title);
				Assert.Equal(post.Content, ((Post)((JsonResult)result).Value).Content);
			}
		}

		[Fact]
		public async Task InvalidCreatePost()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();
			var empty_post = new Post
			{
				BlogId = 1,
				Title = "",
				Content = ""
			};
			var no_blog_post = new Post
			{
				BlogId = 777,
				Title = "xx",
				Content = "yy"
			};
			ActionResult result;

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				// Act
				result = await controller.PostAction(1, true, empty_post, PostActionEnum.AddPost);
				// Assert
				Assert.NotNull(result);
				Assert.IsType<BadRequestObjectResult>(result);

				// Act
				result = await controller.PostAction(1, true, no_blog_post, PostActionEnum.AddPost);
				// Assert
				Assert.NotNull(result);
				Assert.IsType<NotFoundResult>(result);
			}
		}

		[Fact]
		public async Task PostEdit()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				var post = new Post
				{
					BlogId = 1,
					PostId = 1,
					Title = "changed title",
					Content = "changed content"
				};

				// Act
				var result = await controller.PostAction(1, true, post, PostActionEnum.EditPost);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<Post>(((JsonResult)result).Value);
				Assert.Equal(1, ((Post)((JsonResult)result).Value).BlogId);
				Assert.Equal(1, ((Post)((JsonResult)result).Value).PostId);
				Assert.Equal(post.Title, ((Post)((JsonResult)result).Value).Title);
				Assert.Equal(post.Content, ((Post)((JsonResult)result).Value).Content);



				// Arrange
				//add one object
				await repository.AddAsync(new Blog
				{
					BlogId = 2,
					Post = new List<Post>(new[] { new Post
					{
						BlogId = 2,
						PostId = 1,
						Title = "title internet post",
						Content = "content internet post"
					} }),
					Url = "http://www.internet.com",
				});
				await repository.SaveAsync();

				post = new Post
				{
					BlogId = 2,
					PostId = 1,
					Title = "changed title internet post",
					Content = "changed content internet post"
				};

				// Act
				result = await controller.PostAction(2, true, post, PostActionEnum.EditPost);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<Post>(((JsonResult)result).Value);
				Assert.Equal(2, ((Post)((JsonResult)result).Value).BlogId);
				Assert.Equal(1, ((Post)((JsonResult)result).Value).PostId);
				Assert.Equal(post.Title, ((Post)((JsonResult)result).Value).Title);
				Assert.Equal(post.Content, ((Post)((JsonResult)result).Value).Content);
			}
		}

		[Fact]
		public async Task InvalidPostEdit()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				var empty_post = new Post
				{
					BlogId = 1,
					PostId = 1,
					Title = "",
					Content = ""
				};
				var bad_blog_id = new Post
				{
					BlogId = 999,
					PostId = 1,
					Title = "xx",
					Content = "yy"
				};
				var bad_post_id = new Post
				{
					BlogId = 1,
					PostId = 888,
					Title = "xx",
					Content = "yy"
				};

				// Act
				var result = await controller.PostAction(1, true, empty_post, PostActionEnum.EditPost);
				// Assert
				Assert.IsType<BadRequestObjectResult>(result);

				// Act
				result = await controller.PostAction(bad_blog_id.BlogId, true, bad_blog_id, PostActionEnum.EditPost);
				// Assert
				Assert.IsType<NotFoundResult>(result);

				// Act
				result = await controller.PostAction(1, true, bad_post_id, PostActionEnum.EditPost);
				// Assert
				Assert.IsType<NotFoundResult>(result);


				// Arrange
				//add one object
				await repository.AddAsync(new Blog
				{
					BlogId = 2,
					Post = new List<Post>(new[] { new Post
					{
						BlogId = 2,
						PostId = 1,
						Title = "title internet post",
						Content = "content internet post"
					} }),
					Url = "http://www.internet.com",
				});
				await repository.SaveAsync();

				var post = new Post
				{
					//completely empty
				};

				// Act
				result = await controller.PostAction(2, true, post, PostActionEnum.EditPost);

				// Assert
				Assert.IsType<BadRequestObjectResult>(result);
			}
		}

		[Fact]
		public async Task PostDelete()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();

			//1st add one object
			await repository.AddAsync(new Blog
			{
				BlogId = 2,
				Post = new List<Post>(new[] { new Post
					{
						BlogId = 2,
						PostId = 1,
						Title = "title internet post",
						Content = "content internet post"
					},
					new Post
					{
						BlogId = 2,
						PostId = 2,
						Title = "T moar",
						Content = "C moar"
					}
				}),
				Url = "http://www.internet.com",
			});
			await repository.SaveAsync();

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				var post = new Post
				{
					BlogId = 1,
					PostId = 1
				};

				// Act
				var result = await controller.PostAction(1, true, post, PostActionEnum.DeletePost);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<string>(((JsonResult)result).Value);
				Assert.Equal("deleted post", ((JsonResult)result).Value.ToString());

				result = await controller.GetPosts(1);
				//non posts left
				Assert.IsType<JsonResult>(result);
				Assert.IsAssignableFrom<IEnumerable<Post>>(((JsonResult)result).Value);
				Assert.Empty((IEnumerable<Post>)((JsonResult)result).Value);



				post = new Post
				{
					BlogId = 2,
					PostId = 1
				};

				// Act
				result = await controller.PostAction(2, true, post, PostActionEnum.DeletePost);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<string>(((JsonResult)result).Value);
				Assert.Equal("deleted post", ((JsonResult)result).Value.ToString());

				result = await controller.GetPosts(2);
				//1 post left
				Assert.IsType<JsonResult>(result);
				Assert.IsAssignableFrom<IEnumerable<Post>>(((JsonResult)result).Value);
				Assert.NotEmpty((IEnumerable<Post>)((JsonResult)result).Value);


				post = new Post
				{
					BlogId = 2,
					PostId = 2
				};

				// Act
				result = await controller.PostAction(2, true, post, PostActionEnum.DeletePost);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<string>(((JsonResult)result).Value);
				Assert.Equal("deleted post", ((JsonResult)result).Value.ToString());

				result = await controller.GetPosts(2);
				//non posts left
				Assert.IsType<JsonResult>(result);
				Assert.IsAssignableFrom<IEnumerable<Post>>(((JsonResult)result).Value);
				Assert.Empty((IEnumerable<Post>)((JsonResult)result).Value);
			}
		}

		[Fact]
		public async Task InvalidPostDelete()
		{
			// Arrange
			Moq.Mock<IBloggingRepository> mock = MockBloggingRepository();
			IBloggingRepository repository = mock.Object;
			var logger = LoggerFactory.CreateLogger<BlogsController>();

			//1st add one object
			await repository.AddAsync(new Blog
			{
				BlogId = 2,
				Post = new List<Post>(new[] { new Post
					{
						BlogId = 2,
						PostId = 1,
						Title = "title internet post",
						Content = "content internet post"
					},
					new Post
					{
						BlogId = 2,
						PostId = 2,
						Title = "T moar",
						Content = "C moar"
					}
				}),
				Url = "http://www.internet.com",
			});
			await repository.SaveAsync();

			using (IBlogsController controller = new BlogsController(repository, logger, Configuration, DataProtectionProvider))
			{
				var bad_blog_id = new Post
				{
					BlogId = 999,
					PostId = 1
				};
				var bad_post_id = new Post
				{
					BlogId = 1,
					PostId = 888
				};
				var empty = new Post
				{
					//completely empty
				};

				// Act
				var result = await controller.PostAction(bad_blog_id.BlogId, true, bad_blog_id, PostActionEnum.DeletePost);
				// Assert
				Assert.IsType<NotFoundResult>(result);

				// Act
				result = await controller.PostAction(bad_post_id.BlogId, true, bad_post_id, PostActionEnum.DeletePost);
				// Assert
				Assert.IsType<NotFoundResult>(result);

				// Act
				result = await controller.PostAction(empty.BlogId, true, empty, PostActionEnum.DeletePost);
				// Assert
				Assert.IsType<BadRequestObjectResult>(result);
			}
		}
	}
}
