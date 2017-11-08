using AspNetCore.ExistingDb.Repositories;
using AspNetCore.ExistingDb.Tests;
using EFGetStarted.AspNetCore.ExistingDb.Controllers;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

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
					Post = new []
					{
						new Post
						{
							BlogId = 1,
							Content = "content",
							PostId = 1,
							Title = "title"
						}
					},
					Url = "http://www.some-url.com"
				}
			};
			Blog to_add = null;


			mock.Setup(r => r.GetAllAsync()).Returns(() =>
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

			mock.Setup(r => r.GetSingleAsync(Moq.It.IsIn(new object[] { 2 }))).Returns<object[]>((s) =>
			{
				var found = lst.FirstOrDefault(_ => _.BlogId == (int)s.FirstOrDefault());
				return Task.FromResult(found);
			});

			mock.Setup(r => r.GetSingleAsync(Moq.It.IsNotIn(new object[] { 2 }))).Returns(() =>
			{
				return Task.FromResult<Blog>(null);
			});

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
				Assert.Equal(BlogsController.ASPX, ((RedirectResult)result).Url);
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
				var result = await controller.ItemAction(model, true, BlogActionEnum.Edit);

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

				var result = await controller.ItemAction(model, true, BlogActionEnum.Edit);

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
				var result = await controller.ItemAction(model, true, BlogActionEnum.Delete);

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
				BlogId = 99999,
				Post = null,
				Url = "http://www.99999.com",
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
				var result = await controller.ItemAction(model, true, BlogActionEnum.Delete);

				// Assert
				Assert.IsType<NotFoundResult>(result);
				Assert.Equal(404, ((NotFoundResult)result).StatusCode);
			}
		}
	}
}
