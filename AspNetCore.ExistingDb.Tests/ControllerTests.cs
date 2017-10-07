using AspNetCore.ExistingDb.Repositories;
using AspNetCore.ExistingDb.Tests;
using EFGetStarted.AspNetCore.ExistingDb.Controllers;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Controllers
{
	public class BlogsControllers : IClassFixture<BloggingContextDBFixture>
	{
		BloggingContextDBFixture DBFixture { get; set; }

		ILoggerFactory LoggerFactory { get; set; }

		IConfiguration Configuration { get; set; }

		IDataProtectionProvider DataProtectionProvider { get; set; }

		IConfiguration CreateConfiguration()
		{
			var builder = new ConfigurationBuilder()
				//.SetBasePath("wwww")
				.AddJsonFile(@"..\..\AspNetCore.ExistingDb\appsettings.json", optional: true, reloadOnChange: true)
				//.AddJsonFile($@"..\..\AspNetCore.ExistingDb\appsettings.{env.EnvironmentName}.json", optional: true)
				.AddEnvironmentVariables();
			//if (env.IsDevelopment())
			//	builder.AddUserSecrets<Startup>();
			return builder.Build();
		}

		void SetupServices()
		{
			var serviceCollection = new ServiceCollection()
				.AddLogging();
			serviceCollection.AddDataProtection();

			var serviceProvider = serviceCollection.BuildServiceProvider();

			var factory = serviceProvider.GetService<ILoggerFactory>();
			LoggerFactory = factory;

			var protection = serviceProvider.GetService<IDataProtectionProvider>();
			DataProtectionProvider = protection;

			var configuration = CreateConfiguration();
			Configuration = configuration;
		}

		private static Moq.Mock<IBloggingRepository> MockBloggingRepository()
		{
			var mock = new Moq.Mock<IBloggingRepository>();

			mock.Setup(r => r.GetAllAsync()).Returns(() =>
			{
				var lst = new List<Blog>(5)
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
				return Task.FromResult(lst.ToList());
			});

			mock.Setup(r => r.AddAsync(Moq.It.IsAny<Blog>())).Returns(() =>
			{
				return Task.FromResult(new Blog
				{
					BlogId = 2,
					Post = null,
					Url = "http://www.internet.com",
				});
			});

			mock.Setup(r => r.SaveAsync()).Returns(() =>
			{
				return Task.FromResult(1);
			});

			mock.Setup(r => r.GetSingleAsync(Moq.It.IsIn(new int[] { 2 }))).Returns(() =>
			{
				return Task.FromResult(new Blog
				{
					BlogId = 2,
					Post = null,
					Url = "http://www.internet.com",
				});
			});

			mock.Setup(r => r.GetSingleAsync(Moq.It.IsNotIn(new int[] { 2 }))).Returns(() =>
			{
				return Task.FromResult<Blog>(null);
			});

			mock.Setup(r => r.Edit(Moq.It.IsAny<Blog>()));

			return mock;
		}

		public BlogsControllers(BloggingContextDBFixture dBFixture)
		{
			DBFixture = dBFixture;

			SetupServices();
		}

		[Fact]
		async Task Index_GetAll()
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

				//System.Linq.Enumerable.SelectListIterator<EFGetStarted.AspNetCore.ExistingDb.Models.Blog, EFGetStarted.AspNetCore.ExistingDb.Models.DecoratedBlog> xxxxxxxxxx;

				Assert.NotEmpty(((IEnumerable<DecoratedBlog>)((ViewResult)result).Model));
				Assert.Equal(1, ((IEnumerable<DecoratedBlog>)((ViewResult)result).Model).First().BlogId);
				Assert.Equal("http://www.some-url.com", ((IEnumerable<DecoratedBlog>)((ViewResult)result).Model).First().Url);
			}
		}

		[Fact]
		async Task Create()
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
				Assert.Equal("Blogs", ((RedirectResult)result).Url);
			}
		}

		[Fact]
		async Task InvalidCreate()
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
		async Task Edit()
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
		async Task InvalidEdit()
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
		async Task Delete()
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
		async Task InvalidDelete()
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
