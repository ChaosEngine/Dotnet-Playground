using AspNetCore.ExistingDb.Tests;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mime;
using Xunit;

namespace RazorPages
{
	public class WebCamGalleryTests : BaseControllerTest
	{
		PageContext LocalPageContext
		{
			get
			{
				var http_context = new DefaultHttpContext();
				var route_data = new RouteData();
				var page_act_desc = new PageActionDescriptor();
				var model_state_dict = new ModelStateDictionary();
				var action_ctx = new ActionContext(http_context, route_data, page_act_desc, model_state_dict);

				var page_context = new PageContext(action_ctx);
				//var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model_state_dict);
				//var tempData = new TempDataDictionary(http_context, Moq.Mock.Of<ITempDataProvider>());
				//var url = new UrlHelper(action_ctx);
				return page_context;
			}
		}

		public WebCamGalleryTests() : base()
		{
			SetupServices();
		}

		[Fact]
		public void OnGetTest()
		{
			//Arrange
			var serverTiming_mock = new Moq.Mock<Lib.AspNetCore.ServerTiming.IServerTiming>();
			serverTiming_mock.SetupGet(m => m.Metrics).Returns(() =>
			{
				return new List<Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric>();
			});
			WebCamGallery wcg = new WebCamGallery(base.Configuration, serverTiming_mock.Object)
			{
				PageContext = this.LocalPageContext
			};

			//Act
			wcg.OnGet();

			//Assert
			if (!string.IsNullOrEmpty(Configuration["ImageDirectory"]))
			{
				Assert.IsAssignableFrom<IEnumerable<FileInfo>>(wcg.ThumbnailJpgs);
				Assert.NotNull(wcg.ThumbnailJpgs);
				Assert.NotEmpty(wcg.ThumbnailJpgs);
				DateTime? date = null;
				Assert.All(wcg.ThumbnailJpgs, (j) =>
				{
					Assert.IsType<FileInfo>(j);
					if (date.HasValue)
						Assert.True(j.LastWriteTime < date.Value);
					else
						date = j.LastWriteTime;
				});
			}
			Assert.NotNull(wcg.BaseWebCamURL);
		}

		[Theory]
		[InlineData("out-1.jpg")]
		[InlineData("thumbnail-1.jpg")]
		public void OnImageGetTest(string imageName)
		{
			//Arrange
			var serverTiming_mock = new Moq.Mock<Lib.AspNetCore.ServerTiming.IServerTiming>();
			serverTiming_mock.SetupGet(m => m.Metrics).Returns(() =>
			{
				return new List<Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric>();
			});
			WebCamImagesModel wcim = new WebCamImagesModel()
			{
				PageContext = this.LocalPageContext
			};

			//Act
			var result = wcim.OnGet(base.Configuration, serverTiming_mock.Object, imageName);

			//Assert
			if (!string.IsNullOrEmpty(Configuration["ImageDirectory"]))
			{
				Assert.NotNull(result);
				Assert.IsType<PhysicalFileResult>(result);
				Assert.Equal(MediaTypeNames.Image.Jpeg, ((PhysicalFileResult)result).ContentType);
				Assert.NotNull(((PhysicalFileResult)result).EntityTag);
				//Assert.NotNull(((PhysicalFileResult)result).LastModified);
			}


			//test strong caching with ETAG and date tag checking
			if (!string.IsNullOrEmpty(Configuration["ImageDirectory"]))
			{
				//Arrange
				var fi = new FileInfo(Path.Combine(Configuration["ImageDirectory"], imageName));
				DateTimeOffset last = fi.LastWriteTime;
				long etagHash = new DateTimeOffset(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, last.Offset)
					.ToUniversalTime().ToFileTime() ^ fi.Length;
				var etag_str = '\"' + Convert.ToString(etagHash, 16) + '\"';
				wcim.Request.Headers.Add(HeaderNames.IfNoneMatch, new StringValues(etag_str));

				//Act
				result = wcim.OnGet(base.Configuration, serverTiming_mock.Object, imageName);

				//Assert			
				Assert.NotNull(result);
				Assert.IsType<StatusCodeResult>(result);
				Assert.Equal((int)HttpStatusCode.NotModified, ((StatusCodeResult)result).StatusCode);
			}
		}

		[Theory]
		[InlineData("bad.jpg", @"c:\blabla.txt", @"..\..\..\config.json")]
		public void On_NonExisting_ImageGetTest(params string[] badImageNames)
		{
			//Arrange
			var serverTiming_mock = new Moq.Mock<Lib.AspNetCore.ServerTiming.IServerTiming>();
			serverTiming_mock.SetupGet(m => m.Metrics).Returns(() =>
			{
				return new List<Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric>();
			});
			WebCamImagesModel wcim = new WebCamImagesModel()
			{
				PageContext = this.LocalPageContext
			};

			//Act
			foreach (var image_name in badImageNames)
			{
				var result = wcim.OnGet(base.Configuration, serverTiming_mock.Object, image_name);

				//Assert
				Assert.NotNull(result);
				if (!string.IsNullOrEmpty(Configuration["ImageDirectory"]))
					Assert.IsType<NotFoundResult>(result);
				else
					Assert.IsType<NotFoundObjectResult>(result);
			}
		}
	}
}
