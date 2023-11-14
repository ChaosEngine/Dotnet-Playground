using DotnetPlayground.Repositories;
using DotnetPlayground.Services;
using DotnetPlayground.Tests;
using DotnetPlayground.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using WebControllers=DotnetPlayground.Controllers;

namespace Controllers
{
	static class HashesHelpers
	{
		internal static string Sha256(string value)
		{
			StringBuilder sb = new StringBuilder();
			using (var hash = SHA256.Create())
			{
				Encoding enc = Encoding.UTF8;
				Byte[] result = hash.ComputeHash(enc.GetBytes(value));
				foreach (Byte b in result)
					sb.Append(b.ToString("x2"));
			}
			return sb.ToString();
		}

		internal static string MD5(string value)
		{
			StringBuilder sb = new StringBuilder();
			using (var hash = System.Security.Cryptography.MD5.Create())
			{
				Encoding enc = Encoding.UTF8;
				Byte[] result = hash.ComputeHash(enc.GetBytes(value));
				foreach (Byte b in result)
					sb.Append(b.ToString("x2"));
			}
			return sb.ToString();
		}

		internal static Moq.Mock<IHashesRepository> MockHashesRepository()
		{
			var mock = new Moq.Mock<IHashesRepository>();
			string ilfad = "ilfad", alphabet = "abcdefghijklmopqrstuvwxyz";
			var hashes = new List<ThinHashes>(3)
			{
				new ThinHashes
				{
					Key = ilfad ,
					HashMD5 = MD5(ilfad ),
					HashSHA256 = Sha256(ilfad )
				}
			};
			HashesInfo hi = new HashesInfo
			{
				ID = 0,
				IsCalculating = false
			};

			mock.Setup(r => r.SetReadOnly(Moq.It.IsAny<bool>()));

			mock.Setup(r => r.CalculateHashesInfo(Moq.It.IsAny<ILogger>(), Moq.It.IsAny<DbContextOptions<BloggingContext>>(), default))
				.Returns(() =>
				{
					return Task.FromResult(hi);
				})
				.Callback(() =>
				{
					hi.ID = 0;
					hi.Alphabet = alphabet;
					hi.Count = (int)Math.Pow(alphabet.Length, ilfad.Length);
					hi.IsCalculating = false;
					hi.KeyLength = ilfad.Length;
				});

			mock.SetupGet(r => r.CurrentHashesInfo).Returns(() =>
			{
				return Task.FromResult(hi);
			});

			mock.Setup(r => r.FindByAsync(Moq.It.IsAny<Expression<Func<ThinHashes, bool>>>())).Returns<Expression<Func<ThinHashes, bool>>>((s) =>
			{
				var expr = s.Compile();
				var found = hashes.Where(h => expr.Invoke(h)).ToList();
				return Task.FromResult(found);
			});

			mock.Setup(r => r.AutoComplete(Moq.It.IsAny<string>())).Returns<string>((s) =>
			{
				var found = hashes.Where(h =>
					h.HashMD5.ToLowerInvariant().StartsWith(s) || h.HashSHA256.ToLowerInvariant().StartsWith(s) || h.Key.ToLowerInvariant().StartsWith(s))
					.DefaultIfEmpty(new ThinHashes { Key = "nothing found" }).ToList();

				return Task.FromResult(found);
			});

			mock.Setup(r => r.AddRangeAsync(Moq.It.IsAny<IEnumerable<ThinHashes>>())).Returns<IEnumerable<ThinHashes>>((s) =>
			{
				hashes.AddRange(s);

				return Task.FromResult(0);
			});

			mock.Setup(r => r.PagedSearchAsync(Moq.It.IsAny<string>(), Moq.It.IsAny<string>(), Moq.It.IsAny<string>(),
				Moq.It.IsAny<int>(), Moq.It.IsAny<int>(), Moq.It.IsAny<CancellationToken>()))
				.Returns((string sort, string order, string search, int offset, int limit, CancellationToken token) =>
				{
					token.ThrowIfCancellationRequested();

					var column_names = WebControllers.BaseController<ThinHashes>.AllColumnNames;

					IEnumerable<ThinHashes> items = WebControllers.BaseController<ThinHashes>.SearchItems(hashes.AsQueryable(), search, column_names);

					(IEnumerable<ThinHashes> Itemz, int Count) orgtuple =
						WebControllers.BaseController<ThinHashes>.ItemsToJson(items, sort, order, limit, offset);
					(IEnumerable<ThinHashes> Itemz, int Count) tuple =
						(orgtuple.Itemz, orgtuple.Count);

					return Task.FromResult(tuple);
				});

			mock.Setup(r => r.SearchAsync(Moq.It.IsAny<HashInput>())).Returns((HashInput inp) =>
			{
				ThinHashes th;
				if (inp.Kind == KindEnum.MD5)
					th = hashes.FirstOrDefault(x => x.HashMD5 == inp.Search);
				else
					th = hashes.FirstOrDefault(x => x.HashSHA256 == inp.Search);

				return Task.FromResult(th ?? new ThinHashes { Key = "nothing found" });
			});

			mock.Setup(r => r.GetAll()).Returns(() =>
			{
				var querable = hashes.AsQueryable();
				return querable;
			});

			return mock;
		}

		internal static ControllerContext MockContollerContext()
		{
			var httpContextMock = new Moq.Mock<HttpContext>();

			httpContextMock.Setup(r => r.RequestServices.GetService(typeof(DbContextOptions<BloggingContext>))).Returns(() =>
			{
				var opts = new DbContextOptions<BloggingContext>();
				return opts;
			});
			httpContextMock.Setup(r => r.RequestServices.GetService(typeof(ITempDataDictionaryFactory))).Returns(() =>
			{
				var tdp = new Moq.Mock<ITempDataProvider>();
				tdp.Setup(p => p.LoadTempData(Moq.It.IsAny<HttpContext>())).Returns(() =>
				{
					return new Dictionary<string, object>();
				});

				var tddf = new Moq.Mock<ITempDataDictionaryFactory>();
				tddf.Setup(f => f.GetTempData(Moq.It.IsAny<HttpContext>())).Returns(() =>
				{
					return new TempDataDictionary(httpContextMock.Object, tdp.Object);
				});
				return tddf.Object;
			});

			CancellationToken token = default;
			httpContextMock.SetupProperty(r => r.RequestAborted, token).SetReturnsDefault(token);

			httpContextMock.Setup(r => r.RequestServices.GetService(typeof(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator))).Returns(() =>
			{
				var omv = new Moq.Mock<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IObjectModelValidator>();
				omv.Setup(p => p.Validate(Moq.It.IsAny<ActionContext>(),
					Moq.It.IsAny<Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidationStateDictionary>(),
					Moq.It.IsAny<string>(), Moq.It.IsAny<object>()))
					.Callback(() =>
					{
					});
				return omv.Object;
			});

			var cc = new ControllerContext(new ActionContext(httpContextMock.Object, new RouteData(), new ControllerActionDescriptor()));
			return cc;
		}

		internal static Moq.Mock<IBackgroundTaskQueue> MockBackgroundTaskQueue(IHashesRepository repository)
		{
			// Arrange
			var workItems = new System.Collections.Concurrent.ConcurrentQueue<IBaseBackgroundOperation>();
			Moq.Mock<IBackgroundTaskQueue> back_tasks_mock = new Moq.Mock<IBackgroundTaskQueue>();
			Moq.Mock<IBackgroundOperationService> serv_mock = new Moq.Mock<IBackgroundOperationService>();

			serv_mock.Setup(r => r.StartAsync(Moq.It.IsAny<CancellationToken>()))
				.Returns<CancellationToken>(async (token) =>
				{
					foreach (var wi in workItems)
					{
						switch (wi)
						{
							case CalculateHashesInfoBackgroundOperation calc_hash_op:
								await repository.CalculateHashesInfo(null, null, default);
								if (workItems.Contains(wi))
								{
									if (back_tasks_mock.Object != null && back_tasks_mock.Object is IBackgroundTaskQueue back_tasks)
									{
										var oper = await back_tasks.DequeueAsync(token);
										//workItems.TryDequeue(out var dummy);
									}
								}
								break;

							case BeepBackgroundOperation beep:
								await beep.DoWorkAsync(null, token);
								break;

							case DummyBackgroundOperation dummy:
								await dummy.DoWorkAsync(null, token);
								break;

							case YouTubeUploadOperation youtube:
								await youtube.DoWorkAsync(null, token);
								break;

							default:
								throw new NotSupportedException();
						}
					}
				});

			serv_mock.Setup(r => r.StopAsync(Moq.It.IsAny<CancellationToken>()))
				.Returns<CancellationToken>((token) => Task.CompletedTask);

			// Arrange
			var operation = new CalculateHashesInfoBackgroundOperation();

			back_tasks_mock.Setup(r => r.QueueBackgroundWorkItem(Moq.It.IsAny<IBaseBackgroundOperation>()))
				.Callback<IBaseBackgroundOperation>((oper) =>
				{
					workItems.Enqueue(oper);

					serv_mock.Object.StartAsync(default);
				});

			back_tasks_mock.Setup(r => r.DequeueAsync(Moq.It.IsAny<CancellationToken>()))
				.Returns<CancellationToken>((token) =>
				{
					if (workItems.TryDequeue(out var oper))
						return Task.FromResult(oper);
					else
						return null;
				});

			return back_tasks_mock;
		}
	}

	public class HashesController : BaseControllerTest
	{
		private ILogger<WebControllers.HashesController> Logger
		{
			get
			{
				return LoggerFactory.CreateLogger<WebControllers.HashesController>();
			}
		}

		public HashesController() : base()
		{
			SetupServices();
		}

		[Fact]
		public async Task Index()
		{
			// Arrange
			Moq.Mock<IHashesRepository> mock = HashesHelpers.MockHashesRepository();
			IHashesRepository repository = mock.Object;
			Moq.Mock<IBackgroundTaskQueue> backgroundtaskqueue_mock = HashesHelpers.MockBackgroundTaskQueue(repository);

			using (var controller = new WebControllers.HashesController(repository, Logger, backgroundtaskqueue_mock.Object))
			{
				((Controller)controller).ControllerContext = HashesHelpers.MockContollerContext();

				// Act
				var result = await controller.Index();

				// Assert
				Assert.NotNull(result);

				Assert.IsType<ViewResult>(result);
				Assert.Null(((ViewResult)result).Model);
				Assert.Equal("Index", ((ViewResult)result).ViewName);

				var chi = await repository.CurrentHashesInfo;
				Assert.False(chi.IsCalculating);
				Assert.True(chi.Count > 0);
			}
		}

		[Fact]
		public async Task Search_FOUND()
		{
			// Arrange
			Moq.Mock<IHashesRepository> mock = HashesHelpers.MockHashesRepository();
			IHashesRepository repository = mock.Object;
			Moq.Mock<IBackgroundTaskQueue> backgroundtaskqueue_mock = HashesHelpers.MockBackgroundTaskQueue(repository);


			using (var controller = new WebControllers.HashesController(repository, Logger, backgroundtaskqueue_mock.Object))
			{
				// Act
				var result = await controller.Search(new HashInput
				{
					Kind = KindEnum.MD5,
					Search = "b25319faaaea0bf397b2bed872b78c45",
				}, true);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<Hashes>(((JsonResult)result).Value);
				Assert.Equal("ilfad", ((Hashes)((JsonResult)result).Value).Key);
				Assert.Equal("b25319faaaea0bf397b2bed872b78c45", ((Hashes)((JsonResult)result).Value).HashMD5);
				Assert.Equal("1b3d50ffed54e382f06578f5f917ae948ed38e3db2c66ca6e5d07809ba50fe39", ((Hashes)((JsonResult)result).Value).HashSHA256);

				// Act
				result = await controller.Search(new HashInput
				{
					Kind = KindEnum.SHA256,
					Search = "1b3d50ffed54e382f06578f5f917ae948ed38e3db2c66ca6e5d07809ba50fe39",
				}, true);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<Hashes>(((JsonResult)result).Value);
				Assert.Equal("ilfad", ((Hashes)((JsonResult)result).Value).Key);
				Assert.Equal("b25319faaaea0bf397b2bed872b78c45", ((Hashes)((JsonResult)result).Value).HashMD5);
				Assert.Equal("1b3d50ffed54e382f06578f5f917ae948ed38e3db2c66ca6e5d07809ba50fe39", ((Hashes)((JsonResult)result).Value).HashSHA256);
			}
		}

		[Fact]
		public async Task Search_NOTFOUND()
		{
			// Arrange
			Moq.Mock<IHashesRepository> mock = HashesHelpers.MockHashesRepository();
			IHashesRepository repository = mock.Object;
			Moq.Mock<IBackgroundTaskQueue> backgroundtaskqueue_mock = HashesHelpers.MockBackgroundTaskQueue(repository);


			using (var controller = new WebControllers.HashesController(repository, Logger, backgroundtaskqueue_mock.Object))
			{
				// Act
				var result = await controller.Search(new HashInput
				{
					Kind = KindEnum.MD5,
					Search = "notfoundnotfoundnotfoundnotfound",
				}, true);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<Hashes>(((JsonResult)result).Value);
				Assert.Equal("nothing found", ((Hashes)((JsonResult)result).Value).Key);
				Assert.Null(((Hashes)((JsonResult)result).Value).HashMD5);
				Assert.Null(((Hashes)((JsonResult)result).Value).HashSHA256);

				// Act
				result = await controller.Search(new HashInput
				{
					Kind = KindEnum.SHA256,
					Search = "badbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadb",
				}, true);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<Hashes>(((JsonResult)result).Value);
				Assert.Equal("nothing found", ((Hashes)((JsonResult)result).Value).Key);
				Assert.Null(((Hashes)((JsonResult)result).Value).HashMD5);
				Assert.Null(((Hashes)((JsonResult)result).Value).HashSHA256);
			}
		}

		[Fact]
		public async Task InvalidSearch()
		{
			// Arrange
			Moq.Mock<IHashesRepository> mock = HashesHelpers.MockHashesRepository();
			IHashesRepository repository = mock.Object;
			Moq.Mock<IBackgroundTaskQueue> backgroundtaskqueue_mock = HashesHelpers.MockBackgroundTaskQueue(repository);


			using (var controller = new WebControllers.HashesController(repository, Logger, backgroundtaskqueue_mock.Object))
			{
				// Act
				((Controller)controller).ModelState.AddModelError("Hash lenght", "Hash lenght is bad");
				var result = await controller.Search(new HashInput
				{
					Kind = KindEnum.MD5,
					Search = "too_short",
				}, true);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<string>(((JsonResult)result).Value);
				Assert.Equal("error", ((JsonResult)result).Value);
			}
			using (var controller = new WebControllers.HashesController(repository, Logger, backgroundtaskqueue_mock.Object))
			{
				// Act
				((Controller)controller).ModelState.AddModelError("Hash lenght", "Hash lenght is bad");
				var result = await controller.Search(new HashInput
				{
					Kind = KindEnum.SHA256,
					Search = "",//empty
				}, true);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<string>(((JsonResult)result).Value);
				Assert.Equal("error", ((JsonResult)result).Value);
			}
			using (var controller = new WebControllers.HashesController(repository, Logger, backgroundtaskqueue_mock.Object))
			{
				// Act
				((Controller)controller).ModelState.AddModelError("Hash lenght", "Hash lenght is bad");
				var result = await controller.Search(new HashInput
				{
					Kind = KindEnum.SHA256,
					Search = null,//null not allowed
				}, true);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<string>(((JsonResult)result).Value);
				Assert.Equal("error", ((JsonResult)result).Value);
			}
			using (var controller = new WebControllers.HashesController(repository, Logger, backgroundtaskqueue_mock.Object))
			{
				// Act
				((Controller)controller).ModelState.AddModelError("Hash lenght and type", "Hash lenght is bad and type is bad");
				var result = await controller.Search(new HashInput
				{
				}, true);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<string>(((JsonResult)result).Value);
				Assert.Equal("error", ((JsonResult)result).Value);
			}
		}

		[Fact]
		public async Task Autocomplete()
		{
			// Arrange
			Moq.Mock<IHashesRepository> mock = HashesHelpers.MockHashesRepository();
			IHashesRepository repository = mock.Object;
			Moq.Mock<IBackgroundTaskQueue> backgroundtaskqueue_mock = HashesHelpers.MockBackgroundTaskQueue(repository);


			await repository.AddRangeAsync(new[]
			{
				new ThinHashes { Key = "aaaaa", HashMD5 = "594f803b380a41396ed63dca39503542", HashSHA256 = "ed968e840d10d2d313a870bc131a4e2c311d7ad09bdf32b3418147221f51a6e2" },
				new ThinHashes { Key = "aaaab", HashMD5 = "11649b4394d09e4aba132ad49bd1e7db", HashSHA256 = "5ef1b1016a260f0c229c5b24afe87fe24a68b4c80f6f89535b87e0ca72a08623" },
				new ThinHashes { Key = "aaaac", HashMD5 = "16a08135a7d44b3d6beac2d84f9067c6", HashSHA256 = "b3a7dc940ffbb84720f62ede7fc0c59303210e259a5c4c4c85bfc26fb5f04f4d" },
				new ThinHashes { Key = "aaaad", HashMD5 = "fba2fdaf36fdf1931d552535a57eb984", HashSHA256 = "d0977789a5e2f79fdfbb4b1dbb342d90c88eeae3d3c68297a3a3027c859af2ee" },
				new ThinHashes { Key = "aaaae", HashMD5 = "85732438767e17f34cea6e206d2af366", HashSHA256 = "63a094f96b7b890fe1cf798f57465e2f9ab494408564cbf39fcdef159d8697b2" },
				new ThinHashes { Key = "aaaaf", HashMD5 = "148a38bed87a7ddececcdc4dd6a6bb30", HashSHA256 = "06c7c8965b8d5621946c0fe1c80078002c1659c7b6348aaec9be583bf47dac9f" },
				new ThinHashes { Key = "aaaag", HashMD5 = "47ce81bbe7521737555cfbd39e7b6a5e", HashSHA256 = "078b6ba3284302811de5c4a3778a4df5107062f3b8acfb7d0c3705e448a9d761" },
				new ThinHashes { Key = "aaaah", HashMD5 = "ea467513aad73f1820e7e4c8488980b0", HashSHA256 = "eca3e47db80d2151296cf4002c3390c956a50f91bcc156046a949af3aa9ffa58" },
				new ThinHashes { Key = "aaaai", HashMD5 = "65aa471507524e7dde675f0a22ed1287", HashSHA256 = "968c120f2e95ad42b09d289bed97c0db5eb0e21778f87eba48bd044d4534ca25" },
				new ThinHashes { Key = "aaaaj", HashMD5 = "2f52f7168f84e23ea79b9c2ef8d67657", HashSHA256 = "c739af85522b45884bf66294b05fee2efbce602003657ecc4d3df82c9311629a" },
			});

			using (var controller = new WebControllers.HashesController(repository, Logger, backgroundtaskqueue_mock.Object))
			{
				// Act
				var result = await controller.Autocomplete("ilfad", true);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(((JsonResult)result).Value);
				var lst = ((IEnumerable<ThinHashes>)((JsonResult)result).Value);
				Assert.NotNull(lst);
				Assert.NotEmpty(lst);
				Assert.True(1 == lst.Count());
				Assert.Contains(lst, l => l.Key == "ilfad");


				// Act
				result = await controller.Autocomplete("ilf", true);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(((JsonResult)result).Value);
				lst = ((IEnumerable<ThinHashes>)((JsonResult)result).Value);
				Assert.NotNull(lst);
				Assert.NotEmpty(lst);
				Assert.True(1 == lst.Count());
				Assert.Contains(lst, l => l.Key == "ilfad");


				// Act
				result = await controller.Autocomplete("empty", true);

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(((JsonResult)result).Value);
				lst = ((IEnumerable<ThinHashes>)((JsonResult)result).Value);
				Assert.NotNull(lst);
				Assert.NotEmpty(lst);
				Assert.True(1 == lst.Count());
				Assert.Equal("nothing found", lst.First().Key);
				Assert.Null(lst.First().HashMD5);
				Assert.Null(lst.First().HashSHA256);


				// Act
				result = await controller.Autocomplete("078b6ba3284302811d", false);

				// Assert
				Assert.IsType<ViewResult>(result);
				Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(((ViewResult)result).Model);
				lst = ((IEnumerable<ThinHashes>)((ViewResult)result).Model);
				Assert.NotNull(lst);
				Assert.NotEmpty(lst);
				Assert.True(1 == lst.Count());
				Assert.Equal("aaaag", lst.First().Key);
				Assert.Equal("078b6ba3284302811de5c4a3778a4df5107062f3b8acfb7d0c3705e448a9d761", lst.First().HashSHA256);
				Assert.Equal("47ce81bbe7521737555cfbd39e7b6a5e", lst.First().HashMD5);


				// Act
				result = await controller.Autocomplete("e", false);

				// Assert
				Assert.IsType<ViewResult>(result);
				Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(((ViewResult)result).Model);
				lst = ((IEnumerable<ThinHashes>)((ViewResult)result).Model);
				Assert.NotNull(lst);
				Assert.NotEmpty(lst);
				Assert.True(lst.Count() >= 2);
				Assert.True(lst.First().HashMD5.StartsWith("e") || lst.First().HashSHA256.StartsWith("e"));
				Assert.Contains(lst, h =>
					h.HashSHA256 == "ed968e840d10d2d313a870bc131a4e2c311d7ad09bdf32b3418147221f51a6e2"
					|| h.HashSHA256 == "eca3e47db80d2151296cf4002c3390c956a50f91bcc156046a949af3aa9ffa58"
					|| h.HashMD5 == "ea467513aad73f1820e7e4c8488980b0");
			}
		}

		[Fact]
		public async Task InvalidAutocomplete()
		{
			// Arrange
			Moq.Mock<IHashesRepository> mock = HashesHelpers.MockHashesRepository();
			IHashesRepository repository = mock.Object;
			Moq.Mock<IBackgroundTaskQueue> backgroundtaskqueue_mock = HashesHelpers.MockBackgroundTaskQueue(repository);


			using (var controller = new WebControllers.HashesController(repository, Logger, backgroundtaskqueue_mock.Object))
			{
				// Act
				((Controller)controller).ModelState.AddModelError("empty search", "you must specify search keyword");
				var result = await controller.Autocomplete(null, false);

				// Assert
				Assert.IsType<ViewResult>(result);
				Assert.Null(((ViewResult)result).Model);
				Assert.Equal("Index", ((ViewResult)result).ViewName);
			}
		}
	}

	public class HashesDataTableController : BaseControllerTest
	{
		private ILogger<WebControllers.HashesDataTableController> Logger
		{
			get
			{
				return LoggerFactory.CreateLogger<WebControllers.HashesDataTableController>();
			}
		}

		public HashesDataTableController() : base()
		{
			SetupServices();
		}

		[Fact]
		public void Index()
		{
			// Arrange
			Moq.Mock<IHashesRepository> mock = HashesHelpers.MockHashesRepository();
			IHashesRepository repository = mock.Object;


			using (var controller = new WebControllers.VirtualScrollController(repository, Logger, Cache))
			{
				// Act
				var result = controller.Index();

				// Assert
				Assert.IsType<ViewResult>(result);
				Assert.Null(((ViewResult)result).Model);
				Assert.NotNull(((ViewResult)result).ViewName);
			}
		}

		[Fact]
		public async Task Ajaxifiable_Load_Proper()
		{
			// Arrange
			Moq.Mock<IHashesRepository> mock = HashesHelpers.MockHashesRepository();
			IHashesRepository repository = mock.Object;

			await repository.AddRangeAsync(new[]
			{
				//already addded - "ilfad"
				//new ThinHashes { Key = "ilfad", HashMD5 = "b25319faaaea0bf397b2bed872b78c45",  HashSHA256 = "1b3d50ffed54e382f06578f5f917ae948ed38e3db2c66ca6e5d07809ba50fe39" },
				new ThinHashes { Key = "aaaab", HashMD5 = "11649b4394d09e4aba132ad49bd1e7db", HashSHA256 = "5ef1b1016a260f0c229c5b24afe87fe24a68b4c80f6f89535b87e0ca72a08623" },
				new ThinHashes { Key = "aaaab", HashMD5 = "11649b4394d09e4aba132ad49bd1e7db", HashSHA256 = "5ef1b1016a260f0c229c5b24afe87fe24a68b4c80f6f89535b87e0ca72a08623" },
				new ThinHashes { Key = "aaaaa", HashMD5 = "594f803b380a41396ed63dca39503542", HashSHA256 = "ed968e840d10d2d313a870bc131a4e2c311d7ad09bdf32b3418147221f51a6e2" },
				new ThinHashes { Key = "aaaac", HashMD5 = "16a08135a7d44b3d6beac2d84f9067c6", HashSHA256 = "b3a7dc940ffbb84720f62ede7fc0c59303210e259a5c4c4c85bfc26fb5f04f4d" },
				new ThinHashes { Key = "aaaae", HashMD5 = "85732438767e17f34cea6e206d2af366", HashSHA256 = "63a094f96b7b890fe1cf798f57465e2f9ab494408564cbf39fcdef159d8697b2" },
				new ThinHashes { Key = "aaaaf", HashMD5 = null,                               HashSHA256 = "06c7c8965b8d5621946c0fe1c80078002c1659c7b6348aaec9be583bf47dac9f" },
				new ThinHashes { Key = "aaaag", HashMD5 = "47ce81bbe7521737555cfbd39e7b6a5e", HashSHA256 = "078b6ba3284302811de5c4a3778a4df5107062f3b8acfb7d0c3705e448a9d761" },
				new ThinHashes { Key = "aaaaj", HashMD5 = "2f52f7168f84e23ea79b9c2ef8d67657", HashSHA256 = "c739af85522b45884bf66294b05fee2efbce602003657ecc4d3df82c9311629a" },
				new ThinHashes { Key = "aaaah", HashMD5 = "ea467513aad73f1820e7e4c8488980b0", HashSHA256 = "eca3e47db80d2151296cf4002c3390c956a50f91bcc156046a949af3aa9ffa58" },
				new ThinHashes { Key = "aaaai", HashMD5 = "65aa471507524e7dde675f0a22ed1287", HashSHA256 = "968c120f2e95ad42b09d289bed97c0db5eb0e21778f87eba48bd044d4534ca25" },
				new ThinHashes { Key = "aaaad", HashMD5 = "fba2fdaf36fdf1931d552535a57eb984", HashSHA256 = "d0977789a5e2f79fdfbb4b1dbb342d90c88eeae3d3c68297a3a3027c859af2ee" },
			});

			using (var controller = new WebControllers.VirtualScrollController(repository, Logger, Cache))
			{
				((Controller)controller).ControllerContext = HashesHelpers.MockContollerContext();

				// Act - search single
				var result = await controller.Load(new HashesDataTableLoadInput(nameof(ThinHashes.Key), "DESC", "ilfad", 0, 0, "blah"));

				// Assert
				Assert.IsType<JsonResult>(result);
				//fetching anonymous object from JsonResult is weird; use reflection
				Assert.IsType<int>(((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value));
				Assert.Equal(1, (int)((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value));
				Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value));
				Assert.Single((IEnumerable<ThinHashes>)((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value));
				Assert.Equal("ilfad", ((IEnumerable<ThinHashes>)((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value)).ElementAt(0).Key);

				// Act - search by Hash256 dont mind ordering
				result = await controller.Load(new HashesDataTableLoadInput(null, null, "b3a7dc", 0, 0, "blah"));

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<int>(((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value));
				Assert.Equal(1, (int)((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value));
				Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value));
				Assert.Single((IEnumerable<ThinHashes>)((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value));
				Assert.Equal("aaaac", ((IEnumerable<ThinHashes>)((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value)).ElementAt(0).Key);

				// Act - search by Hash256 but not from start - should not find anything
				result = await controller.Load(new HashesDataTableLoadInput(null, null, "e7fc0c5", 0, 0, "blah"));

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<int>(((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value));
				Assert.Equal(0, (int)((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value));
				Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value));
				Assert.Empty((IEnumerable<ThinHashes>)((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value));


				// Act - ensure order by Key desc
				result = await controller.Load(new HashesDataTableLoadInput(nameof(ThinHashes.Key), "DESC", "", 0, 0, "blah"));

				Assert.IsType<int>(((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value));
				Assert.True((int)((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value) > 0);
				Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value));
				Assert.NotEmpty((IEnumerable<ThinHashes>)((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value));
				Assert.Equal("aaaaa", ((IEnumerable<ThinHashes>)((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value)).Last().Key);


				// Act - ensure order by HashSHA256 asc
				result = await controller.Load(new HashesDataTableLoadInput(nameof(ThinHashes.HashSHA256), "ASC", "", 3, 3, "blah"));

				Assert.IsType<int>(((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value));
				Assert.True((int)((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value) > 0);
				Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value));
				Assert.Equal(3, ((IEnumerable<ThinHashes>)((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value)).Count());
				Assert.Equal("aaaab", ((IEnumerable<ThinHashes>)((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value)).First().Key);


				// Act - search unexisting, empty result
				result = await controller.Load(new HashesDataTableLoadInput(nameof(ThinHashes.HashMD5), "ASC", "dummy", 3, 3, "blah"));

				Assert.IsType<int>(((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value));
				Assert.Equal(0, (int)((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value));
				Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value));
				Assert.Empty(((IEnumerable<ThinHashes>)((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value)));

				// Act - order by bad column - should raise an exception
				var exception = Assert.ThrowsAnyAsync<Exception>(async () =>
				{
					result = await controller.Load(new HashesDataTableLoadInput("badbad", "ASC", "dummy", 3, 3, "blah"));
				});


				// Act - get all results no sorting or searching, verify count
				result = await controller.Load(new HashesDataTableLoadInput(null, null, "", 0, 0, "blah"));

				// Assert
				Assert.IsType<JsonResult>(result);
				Assert.IsType<int>(((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value));
				Assert.Equal(12, (int)((JsonResult)result).Value.GetType().GetProperty("total").GetValue(((JsonResult)result).Value));
				Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value));
				Assert.True(12 == ((IEnumerable<ThinHashes>)((JsonResult)result).Value.GetType().GetProperty("rows").GetValue(((JsonResult)result).Value)).Count());
			}
		}

		[Fact]
		public async Task Ajaxifiable_Cancell_Load()
		{
			// Arrange
			Moq.Mock<IHashesRepository> mock = HashesHelpers.MockHashesRepository();
			IHashesRepository repository = mock.Object;

			using (var controller = new WebControllers.VirtualScrollController(repository, Logger, Cache))
			{
				((Controller)controller).ControllerContext = HashesHelpers.MockContollerContext();
				((Controller)controller).HttpContext.RequestAborted = new CancellationToken(true);

				// Act - search single
				var result = await controller.Load(new HashesDataTableLoadInput(nameof(ThinHashes.Key), "DESC", "ilfad", 0, 0, "blah"));

				// Assert
				Assert.IsType<OkResult>(result);
			}
		}

		[Fact]
		public async Task Ajaxifiable_Cancell_ValidationFailed()
		{
			// Arrange
			Moq.Mock<IHashesRepository> mock = HashesHelpers.MockHashesRepository();
			IHashesRepository repository = mock.Object;

			using (var controller = new WebControllers.VirtualScrollController(repository, Logger, Cache))
			{
				((Controller)controller).ControllerContext = HashesHelpers.MockContollerContext();

				var model = new HashesDataTableLoadInput("bad_bad", "WHATEVER", "<>..;[][-", -1, -4, "blah");
				((Controller)controller).ModelState.AddModelError("Sort", "Characters are not allowed: only Key|HashMD5|HashSHA256");
				((Controller)controller).ModelState.AddModelError("Order", "Order not allowed: only asc|desc");
				((Controller)controller).ModelState.AddModelError("Search", "Characters are not allowed");
				((Controller)controller).ModelState.AddModelError("Limit", "bad numeric range");
				((Controller)controller).ModelState.AddModelError("Offset", "bad numeric range");

				// Act - search single
				var result = await controller.Load(model);

				// Assert
				Assert.NotNull(result);
				Assert.IsType<BadRequestObjectResult>(result);
				Assert.NotNull((result as BadRequestObjectResult).Value);
				Assert.IsType<SerializableError>((result as BadRequestObjectResult).Value);
				Assert.NotEmpty(((result as BadRequestObjectResult).Value as SerializableError));
			}
		}
	}
}
