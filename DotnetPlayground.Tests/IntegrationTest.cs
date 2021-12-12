using DotnetPlayground;
using DotnetPlayground.Controllers;
using DotnetPlayground.Models;
using DotnetPlayground.Tests;
using InkBall.IntegrationTests;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Integration
{
	static class Extensions
	{
		public static Dictionary<string, string> ToDictionary(this object myObj)
		{
			return myObj.GetType()
				.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
				.Select(pi => new { pi.Name, Value = pi.GetValue(myObj, null)?.ToString() })
				.Union(
					myObj.GetType()
						.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
						.Select(fi => new { fi.Name, Value = fi.GetValue(myObj)?.ToString() })
				)
				.ToDictionary(ks => ks.Name, vs => vs.Value);
		}
	}

	[Collection(nameof(TestServerCollection))]
	public class HomePage
	{
		private readonly TestServerFixture<Startup> _fixture;
		private readonly HttpClient _client;

		public HomePage(TestServerFixture<Startup> fixture)
		{
			_fixture = fixture;
			_client = fixture.Client;
		}

		[Fact]
		public async Task Index()
		{
			// Arrange
			// Act
			using (HttpResponseMessage response = await _client.GetAsync(_client.BaseAddress))
			{
				// Assert
				response.EnsureSuccessStatusCode();

				var responseString = await response.Content.ReadAsStringAsync();
				Assert.Contains("<title>Home Page - Dotnet Core Playground</title>", responseString);
				Assert.Contains("<h2>Links</h2>", responseString);
			}
		}

		[Fact]
		public async Task ErrorHandlerTest()
		{
			// Arrange
			var payload = new Dictionary<string, string>
			{
				{ "action", "exception" }
			};
			using (var content = new FormUrlEncodedContent(payload))
			{
				// Act
				using (var response = await _client.PostAsync("/DeesNotExist/FooBar", content))
				{
					// Assert
					Assert.NotNull(response);
					Assert.False(response.IsSuccessStatusCode);
					Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
				}
			}
		}

		[Fact]
		public async Task UnintentionalErr()
		{
			// Arrange
			var payload = new Dictionary<string, string>
			{
				{ "action", "exception" }
			};
			using (var content = new FormUrlEncodedContent(payload))
			{
				// Act
				using (var response = await _client.PostAsync($"{_client.BaseAddress}Home/{nameof(HomeController.UnintentionalErr)}", content))
				{
					// Assert
					Assert.NotNull(response);
					Assert.False(response.IsSuccessStatusCode);
					Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
					var responseString = await response.Content.ReadAsStringAsync();
					Assert.Contains("Exception: test exception", responseString);
				}
			}
		}

		[Fact]
		public async Task ClientsideLog()
		{
			// Arrange
			var payload = new Dictionary<string, string>
			{
				{ "level", LogLevel.Warning.ToString() },
				{ "message", "some message" },
				{ "url", "http://localhost/sourceURL" },
				{ "line", 2.ToString() },
				{ "col", 1.ToString() },
				{ "error", "some error" },
			};
			//// Serialize our concrete class into a JSON String
			//var stringPayload = JsonConvert.SerializeObject(payload);
			//// Wrap our JSON inside a StringContent which then can be used by the HttpClient class
			//var content = new StringContent(stringPayload, Encoding.UTF8, "application/json");
			using (var content = new FormUrlEncodedContent(payload))
			{
				// Act
				using (var response = await _client.PostAsync($"{_client.BaseAddress}Home/{nameof(HomeController.ClientsideLog)}", content))
				{
					// Assert
					response.EnsureSuccessStatusCode();
					Assert.NotNull(response);
					Assert.Equal(HttpStatusCode.OK, response.StatusCode);
				}
			}
		}
	}

	[Collection(nameof(TestServerCollection))]
	public class HashesPage
	{
		private readonly TestServerFixture<Startup> _fixture;
		private readonly HttpClient _client;

		public HashesPage(TestServerFixture<Startup> fixture)
		{
			_fixture = fixture;
			_client = fixture.Client;
		}

		[Fact]
		public async Task GET()
		{
			if (_fixture.DOTNET_RUNNING_IN_CONTAINER) return;//pass on fake DB with no data


			//Arrange
			//get number of all total rows from previous tests :-)
			int total_hashes_count = await new HashesDataTablePage(_fixture).Load_Valid("Key", "asc", "aaa", 5, 0, "cached");


			// Arrange
			bool? is_HashesInfo_table_empty = total_hashes_count <= 0 ? true : default;
			string calculating_content_substr = @"<p>Calculating...wait about 10 secs or so...and refresh the page</p>",
				calculated_content_substr = @"<p>
				Search for <strong>([0-9].*)</strong> character MD5 or SHA256 hash source string. Alphabet is '(.*)'
			</p>
			<p>
				Hashes count: <strong>([0-9].*)</strong>
				last updated (.*)
			</p>";

			// Act
			using (HttpResponseMessage response = await _client.GetAsync($"{_client.BaseAddress}{HashesController.ASPX}/"))
			{
				// Assert
				response.EnsureSuccessStatusCode();
				var responseString = await response.Content.ReadAsStringAsync();

				Assert.True(
					responseString.Contains("Hashes count: <strong>") ||
					responseString.Contains(calculating_content_substr)
					);

				var empty_hashes_response_match = Regex.Matches(responseString, calculating_content_substr);
				var hashes_not_empty_response_match = Regex.Matches(responseString, calculated_content_substr);

				Assert.True(
					(empty_hashes_response_match.Count > 0 && hashes_not_empty_response_match.Count <= 0) ||
					(empty_hashes_response_match.Count <= 0 && hashes_not_empty_response_match.Count > 0)
					);

				is_HashesInfo_table_empty = empty_hashes_response_match.Count > 0 && hashes_not_empty_response_match.Count <= 0;
			}

			//if not yet calculated, we wait until it finaly is calculated and assert new page content
			//only hapening if we are sure there are _any records_ inside table hashes
			if (is_HashesInfo_table_empty.HasValue && total_hashes_count > 0)
			{
				// Arrange
				int wait_tries_count = 15;//15x try out

				// Act
				do
				{
					using (HttpResponseMessage response = await _client.GetAsync($"{_client.BaseAddress}{HashesController.ASPX}/"))
					{
						// Assert
						response.EnsureSuccessStatusCode();
						var responseString = await response.Content.ReadAsStringAsync();

						if (responseString.Contains("Hashes count: <strong>"))
						{
							MatchCollection matches_not_empty = Regex.Matches(responseString, calculated_content_substr);
							Assert.NotEmpty(matches_not_empty);
							break;
						}
					}

					await Task.Delay(2_000);
				}
				while (--wait_tries_count > 0);

				Assert.True(wait_tries_count > 0, "not enough tries for HashInfo calculation to succeed");
			}
			else
			{
				//proof that HashesTable is empty
			}
		}

	}

	[Collection(nameof(TestServerCollection))]
	public class HashesDataTablePage
	{
		class TypedResult
		{
			public int total { get; set; }
			public ThinHashes[] rows { get; set; }
		};

		private readonly TestServerFixture<Startup> _fixture;
		private readonly HttpClient _client;

		public HashesDataTablePage(TestServerFixture<Startup> fixture)
		{
			_fixture = fixture;
			_client = fixture.Client;
		}

		[Fact]
		public async Task Index()
		{
			// Arrange
			// Act
			using (HttpResponseMessage response = await _client.GetAsync($"{_client.BaseAddress}{VirtualScrollController.ASPX}/"))
			{
				// Assert
				response.EnsureSuccessStatusCode();

				var responseString = await response.Content.ReadAsStringAsync();
				Assert.Contains("<button id=\"btninfo\" class=\"btn btn-secondary\" type=\"button\" data-bs-toggle=\"modal\" data-bs-target=\"#exampleModal\">&#9432;&nbsp;Row info</button>",
					responseString);
				Assert.Contains("data-page-list=\"[50,500,2000,10000]\"", responseString);
			}
		}

		[Theory]
		[InlineData("Key", "desc", "kawa", 5, 1, "cached")]
		[InlineData("Key", "asc", "awak", 5, 1, "cached")]
		[InlineData("Key", "desc", "kawa", 5, 1, "refresh")]
		[InlineData("Key", "asc", "awak", 5, 1, "refresh")]
		[InlineData("Key", "asc", "none_existing", 5, 1, "cached")]
		[InlineData("Key", "asc", "none_existing", 5, 1, "refresh")]
		public async Task<int> Load_Valid(string sort, string order, string search, int limit, int offset, string extraParam)
		{
			if (_fixture.DOTNET_RUNNING_IN_CONTAINER) return 0;//pass on fake DB with no data


			// Arrange
			var query_input = new HashesDataTableLoadInput
			{
				Sort = sort,
				Order = order,
				Search = search,
				Limit = limit,
				Offset = offset,
				ExtraParam = extraParam,
			}.ToDictionary();
			using (var content = new FormUrlEncodedContent(query_input))
			{
				var queryString = await content.ReadAsStringAsync();
				// Act
				using (HttpResponseMessage response = await _client.GetAsync($"{_client.BaseAddress}{VirtualScrollController.ASPX}/{nameof(HashesDataTableController.Load)}?{queryString}", HttpCompletionOption.ResponseContentRead))
				{
					// Assert
					Assert.NotNull(response);
					response.EnsureSuccessStatusCode();
					Assert.Equal(HttpStatusCode.OK, response.StatusCode);

					var jsonString = await response.Content.ReadAsStringAsync();

					var typed_result = new TypedResult
					{
						total = 1,
						rows = new ThinHashes[] { }
					};

					// Deserialize JSON String into concrete class
					var data = JsonSerializer.Deserialize<TypedResult>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					Assert.IsType(typed_result.GetType(), data);
					Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(data.rows);

					Assert.True(data.rows.Length == 5 || data.rows.Length == 0);
					Assert.True(data.total >= 0);

					if (data.rows.Length > 0)
					{
						Assert.StartsWith(search, data.rows[0].Key);

						if (query_input.TryGetValue("ExtraParam", out string value) && value == "cached")
						{
							Assert.True(response.Headers.CacheControl.Public &&
								response.Headers.CacheControl.MaxAge == DotnetPlayground.Repositories.HashesRepository.HashesInfoExpirationInMinutes);
						}
						else
						{
							Assert.Null(response.Headers.CacheControl?.Public);
						}
					}
					else
					{
						Assert.Null(response.Headers.CacheControl?.Public);
					}

					return data.total;
				}
			}
		}

		[Theory]
		[InlineData("dead", "string", "is", 0xDEAD, 0xBEEF, "refresh")]
		[InlineData("Key", "asc", "awak", 5, 1, "bad")]
		public async Task Load_Invalid(string sort, string order, string search, int limit, int offset, string extraParam)
		{
			// Arrange
			var data = new HashesDataTableLoadInput
			{
				Sort = sort,
				Order = order,
				Search = search,
				Limit = limit,
				Offset = offset,
				ExtraParam = extraParam,
			}.ToDictionary();
			using (var content = new FormUrlEncodedContent(data))
			{
				var queryString = await content.ReadAsStringAsync();
				// Act
				using (HttpResponseMessage response =
					await _client.GetAsync($"{_client.BaseAddress}{VirtualScrollController.ASPX}/{nameof(HashesDataTableController.Load)}?{queryString}",
					HttpCompletionOption.ResponseContentRead))
				{
					// Assert
					Assert.NotNull(response);
					Assert.False(response.IsSuccessStatusCode);
					Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
					Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
				}
			}
		}
	}

	[Collection(nameof(TestServerCollection))]
	public class BlogsPage
	{
		private readonly TestServerFixture<Startup> _fixture;
		private readonly HttpClient _client;

		public BlogsPage(TestServerFixture<Startup> fixture)
		{
			_fixture = fixture;
			_client = fixture.Client;
		}

		[Fact]
		public async Task Show_Index()
		{
			if (_fixture.DOTNET_RUNNING_IN_CONTAINER) return;//pass on fake DB with no data


			// Arrange
			// Act
			using (HttpResponseMessage response = await _client.GetAsync($"{_client.BaseAddress}{BlogsController.ASPX}/"))
			{
				// Assert
				response.EnsureSuccessStatusCode();

				var responseString = await response.Content.ReadAsStringAsync();
				Assert.Contains("<title>Blogs - Dotnet Core Playground</title>", responseString);
				Assert.Contains("js/Blogs.", responseString);//test loading main js script
			}
		}

		[Fact]
		public async Task Show_Create()
		{
			// Arrange
			// Act
			using (HttpResponseMessage response = await _client.GetAsync($"{_client.BaseAddress}{BlogsController.ASPX}/{nameof(BlogsController.Create)}/"))
			{
				// Assert
				response.EnsureSuccessStatusCode();

				var responseString = await response.Content.ReadAsStringAsync();
				Assert.Contains("<title>New Blog - Dotnet Core Playground</title>", responseString);
				Assert.Contains("<label class=\"form-label\" for=\"Url\">Url</label>", responseString);
			}
		}

		[Fact]
		public async Task Blog_CRUD_Test()
		{
			if (_fixture.DOTNET_RUNNING_IN_CONTAINER) return;//pass on fake DB with no data


			// Arrange
			string antiforgery_token;
			List<KeyValuePair<string, string>> data;
			using (var create_get_response = await _client.GetAsync($"{_client.BaseAddress}{BlogsController.ASPX}/{nameof(BlogsController.Create)}/",
				HttpCompletionOption.ResponseContentRead))
			{
				// Assert
				create_get_response.EnsureSuccessStatusCode();
				antiforgery_token = await PostRequestHelper.ExtractAntiForgeryToken(create_get_response);

				// Arrange
				var now = DateTime.Now;
				data = new Blog
				{
					BlogId = 0,
					Post = new[] { new Post { Content = $"aaaa {now}", Title = "titla" } },
					Url = $"http://www.inernetAt-{now.Year}-{now.Month}-{now.Day}.com/Content{now.Hour}-{now.Minute}-{now.Second}"
				}.ToDictionary().ToList();
				data.Add(new KeyValuePair<string, string>("__RequestVerificationToken", antiforgery_token));

				using (var formPostBodyData = new FormUrlEncodedContent(data))
				{
					PostRequestHelper.CreateFormUrlEncodedContentWithCookiesFromResponse(formPostBodyData.Headers, create_get_response);
					// Act
					using (var redirect = await _client.PostAsync($"{_client.BaseAddress}{BlogsController.ASPX}/{nameof(BlogsController.Create)}/", formPostBodyData))
					{
						// Assert
						Assert.NotNull(redirect);
						Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
						Assert.Contains($"/{BlogsController.ASPX}", redirect.Headers.GetValues("Location").FirstOrDefault());
					}
				}


				int last_inserted_id;
				string last_inserted_ProtectedID;
				using (var index_response = await _client.GetAsync($"{_client.BaseAddress}{BlogsController.ASPX}/", HttpCompletionOption.ResponseContentRead))
				{
					var responseString = await index_response.Content.ReadAsStringAsync();
					MatchCollection matches = Regex.Matches(responseString, @"\<form method=""post"" class=""blogForm row g-3"" data-id=""([0-9].*)""\>");
					Assert.NotEmpty(matches);
					var ids = new List<int>(matches.Count);
					foreach (Match m in matches)
					{
						var match_count = m.Success ? m.Groups[1].Captures.Count : 0;
						Assert.True(match_count > 0);
						var id = int.Parse(m.Groups[1].Captures[match_count - 1].Value);
						ids.Add(id);
					}
					last_inserted_id = ids.OrderByDescending(_ => _).First();
					Match match = Regex.Match(responseString, $@"\<input type=""hidden"" id=""ProtectedID_{last_inserted_id}"" name=""ProtectedID"" value=""([^""]+)"" \/\>");
					Assert.True(match.Success && match.Groups[1].Captures.Count > 0);
					last_inserted_ProtectedID = match.Groups[1].Captures[0].Value;
				}

				data = new DecoratedBlog
				{
					BlogId = last_inserted_id,
					ProtectedID = last_inserted_ProtectedID,
					Url = $"http://www.changed-{now.Year + 1}-{now.Month}-{now.Day}.com/NewContent{now.Hour}-{now.Minute}-{now.Second}"
				}.ToDictionary().ToList();
				data.Add(new KeyValuePair<string, string>("__RequestVerificationToken", antiforgery_token));

				using (var formPostBodyData = new FormUrlEncodedContent(data))
				{
					PostRequestHelper.CreateFormUrlEncodedContentWithCookiesFromResponse(formPostBodyData.Headers, create_get_response);
					using (var response = await _client.PostAsync($"{_client.BaseAddress}{BlogsController.ASPX}/{nameof(BlogActionEnum.Edit)}/{last_inserted_id}/true",
						formPostBodyData))
					{
						Assert.NotNull(response);
						response.EnsureSuccessStatusCode();
						Assert.Contains("application/json", response.Content.Headers.GetValues("Content-Type").FirstOrDefault());
						Assert.Contains("{\"blogId\":" + last_inserted_id + ",\"url\":\"" + $"http://www.changed-{now.Year + 1}-{now.Month}-{now.Day}.com/NewContent{now.Hour}-{now.Minute}-{now.Second}",
							await response.Content.ReadAsStringAsync());
					}
				}

				data = new DecoratedBlog
				{
					BlogId = last_inserted_id,
					ProtectedID = last_inserted_ProtectedID,
				}.ToDictionary().ToList();
				data.Add(new KeyValuePair<string, string>("__RequestVerificationToken", antiforgery_token));

				using (var formPostBodyData = new FormUrlEncodedContent(data))
				{
					PostRequestHelper.CreateFormUrlEncodedContentWithCookiesFromResponse(formPostBodyData.Headers, create_get_response);
					using (var response = await _client.PostAsync($"{_client.BaseAddress}{BlogsController.ASPX}/{nameof(BlogActionEnum.Delete)}/{last_inserted_id}/true",
						formPostBodyData))
					{
						Assert.NotNull(response);
						response.EnsureSuccessStatusCode();
						Assert.Contains("application/json", response.Content.Headers.GetValues("Content-Type").FirstOrDefault());
						Assert.Equal("\"deleted\"", await response.Content.ReadAsStringAsync());
					}
				}
			}//end using (var create_get_response
		}
	}

	[Collection(nameof(TestServerCollection))]
	public class WebCamGalleryPage
	{
		private readonly TestServerFixture<Startup> _fixture;
		private readonly HttpClient _client;

		public WebCamGalleryPage(TestServerFixture<Startup> fixture)
		{
			_fixture = fixture;
			_client = fixture.Client;
		}

		[Fact]
		public async Task Show_Index()
		{
			// Arrange
			// Act
			using (HttpResponseMessage response = await _client.GetAsync($"{_client.BaseAddress}{WebCamGallery.ASPX}/"))
			{
				// Assert
				response.EnsureSuccessStatusCode();

				var responseString = await response.Content.ReadAsStringAsync();
				Assert.Contains("<title>WebCam Gallery - Dotnet Core Playground</title>", responseString);
				Assert.Contains("/js/WebCamGallery.", responseString);//test loading main js script
				Assert.Contains("WebCamGalleryOnLoad", responseString);

				if (!string.IsNullOrEmpty(_fixture.ImageDirectory))
				{
					/**
					 <a href="/dotnet/WebCamImages/thumbnail-0.jpg" title="2020-09-14 08:40:01Z">
						<picture>
							<source />
							<img alt='no img' class='inactive' />
						</picture>
					</a>
					<a href="/dotnet/WebCamImages/thumbnail-144.jpg" title="2020-09-14 08:30:01Z">
						<picture>
							<source />
							<img alt='no img' class='inactive' />
						</picture>
					</a>*/

					MatchCollection matches = Regex.Matches(responseString, @"\<a href=""(.*thumbnail-.*\.jpg)"" title=""(.*)"">");
					Assert.NotEmpty(matches);
					var images = new List<string>(matches.Count);
					foreach (Match m in matches.Take(7))
					{
						var match_count = m.Success ? m.Groups[1].Captures.Count : 0;
						Assert.True(match_count > 0);

						var img = m.Groups[1].Captures[match_count - 1].Value;
						images.Add(img);

						var date = m.Groups[2].Captures[match_count - 1].Value;
						Assert.True(DateTime.TryParse(date, out DateTime dt));
					}
					Assert.True(images.Count > 4);
				}
			}
		}

		[Theory]
		[InlineData("out-1.jpg")]
		[InlineData("out-1.webp")]
		[InlineData("out-1.avif")]
		public async Task GetImage(string imageName)
		{
			// Arrange
			// Act
			using (HttpResponseMessage response = await _client.GetAsync($"{_client.BaseAddress}{WebCamImagesModel.ASPX}/{imageName}", HttpCompletionOption.ResponseHeadersRead))
			{
				// Assert
				Assert.NotNull(response);

				if (!string.IsNullOrEmpty(_fixture.ImageDirectory))
				{
					IEnumerable<string> c_type;
					switch (Path.GetExtension(imageName))
					{
						case ".avif":
							response.EnsureSuccessStatusCode();

							Assert.IsType<StreamContent>(response.Content);
							Assert.True(response.Content.Headers.TryGetValues("Content-Type", out c_type));
							Assert.NotNull(c_type);
							Assert.Equal("image/avif", response.Content.Headers.ContentType.MediaType);

							Assert.NotNull(response.Headers.ETag);
							_ = response.Headers.ETag.Tag;
							break;
						case ".webp":
							response.EnsureSuccessStatusCode();

							Assert.IsType<StreamContent>(response.Content);
							Assert.True(response.Content.Headers.TryGetValues("Content-Type", out c_type));
							Assert.NotNull(c_type);
							Assert.Equal("image/webp", response.Content.Headers.ContentType.MediaType);

							Assert.NotNull(response.Headers.ETag);
							_ = response.Headers.ETag.Tag;
							break;
						case ".jpg":
						case ".jpeg":
							response.EnsureSuccessStatusCode();

							Assert.IsType<StreamContent>(response.Content);
							Assert.True(response.Content.Headers.TryGetValues("Content-Type", out c_type));
							Assert.NotNull(c_type);
							Assert.Equal(MediaTypeNames.Image.Jpeg, response.Content.Headers.ContentType.MediaType);

							Assert.NotNull(response.Headers.ETag);
							_ = response.Headers.ETag.Tag;
							break;
						default:
							Assert.False(response.IsSuccessStatusCode, "bad extension");
							break;
					}

				}
				else
				{
					Assert.False(response.IsSuccessStatusCode);
					Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
				}
			}//end using

			/*//test getting the same mage ut with ETAG set - we shoud get HTTP NotModified (304) response code
			if (!string.IsNullOrEmpty(etag))
			{
				// Arrange
				var request = new HttpRequestMessage(HttpMethod.Get, $"{_client.BaseAddress}{WebCamImagesModel.ASPX}/{imageName}");
				//request.Headers.Add(HeaderNames.IfNoneMatch, etag);
				//request.Headers.TryAddWithoutValidation(HeaderNames.ETag, etag);
				request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(etag));

				// Act
				using (HttpResponseMessage response = await _client.SendAsync(request))
				{
					// Assert
					Assert.NotNull(response);
					Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
				}
			}*/
		}

		[Fact]
		public async Task GetLiveImage()
		{
			if (string.IsNullOrEmpty(_fixture.LiveWebCamURL)) return;

			// Arrange
			// Act
			using (HttpResponseMessage response = await _client.GetAsync($"{_client.BaseAddress}{WebCamImagesModel.ASPX}/?handler=live", HttpCompletionOption.ResponseContentRead))
			{
				// Assert
				Assert.NotNull(response);
				response.EnsureSuccessStatusCode();

				Assert.IsType<StreamContent>(response.Content);
				Assert.True(response.Content.Headers.TryGetValues("Content-Type", out IEnumerable<string> c_type));
				Assert.NotNull(c_type);
				Assert.True(response.Headers.TryGetValues("Server-Timing", out var serverTiming_headers));
				Assert.NotEmpty(serverTiming_headers);
				Assert.Matches("GET;dur=([0-9].*);desc=\"live image get\"", serverTiming_headers.First());
				Assert.True(MediaTypeNames.Image.Jpeg == response.Content.Headers.ContentType.MediaType ||
					"image/png" == response.Content.Headers.ContentType.MediaType);

			}//end using
		}
	}

	[Collection(nameof(TestServerCollection))]
	public class IdentityManager2
	{
		private readonly TestServerFixture<Startup> _fixture;
		private readonly HttpClient _client;

		public IdentityManager2(TestServerFixture<Startup> fixture)
		{
			_fixture = fixture;
			_client = fixture.Client;
		}

		[Fact]
		public async Task Show_Index()
		{
			// Arrange
			// Act
			using (HttpResponseMessage response = await _client.GetAsync($"{_client.BaseAddress}assets/Templates.home.html"))
			{
				// Assert
				response.EnsureSuccessStatusCode();

				var responseString = await response.Content.ReadAsStringAsync();
				Assert.Contains("IdentityManager2", responseString);
				Assert.Contains("PathBase", responseString);
			}
		}
	}

	[Collection(nameof(AuthorizedTestingServerCollection))]
	public class AccountAndProfileManagement
	{
		private readonly AuthorizedTestServerFixture _fixture;
		private readonly HttpClient _anonClient;

		public AccountAndProfileManagement(AuthorizedTestServerFixture fixture)
		{
			_fixture = fixture;
			_anonClient = _fixture.AnonymousClient;
		}

		[Theory]
		[InlineData("Identity/Account/Manage/Index")]
		[InlineData("Identity/Account/Manage/ChangePassword")]
		[InlineData("Identity/Account/Manage/ExternalLogins")]
		[InlineData("Identity/Account/Manage/TwoFactorAuthentication")]
		[InlineData("Identity/Account/Manage/PersonalData")]
		public async Task Page_NonAuthenticated_Open(string page)
		{
			// Arrange
			// Act
			using (var response = await _anonClient.GetAsync($"{_anonClient.BaseAddress}{page}"))
			{
				// Assert
				//Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
				Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
				Assert.Equal($"{_anonClient.BaseAddress}Identity/Account/Login?ReturnUrl=%2F{UrlEncoder.Default.Encode(page)}",
					response.Headers.Location.ToString());
			}
		}

		[Theory]
		[InlineData("Identity/Account/Manage/Index", "Phone number")]
		[InlineData("Identity/Account/Manage/ChangePassword", "Confirm new password")]
		//[InlineData("Identity/Account/Manage/ExternalLogins", "Registered Logins")]
		[InlineData("Identity/Account/Manage/TwoFactorAuthentication", "Two-factor authentication")]
		[InlineData("Identity/Account/Manage/PersonalData", "Deleting this data will permanently remove your account")]
		public async Task Pages_Authenticated_Open(string page, string contentToCheck)
		{
			// Arrange
			using var client = await _fixture.CreateAuthenticatedClientAsync();

			using (var request = new HttpRequestMessage(HttpMethod.Get, $"{client.BaseAddress}{page}"))
			{
				// Act
				using (var response = await client.SendAsync(request))
				{
					// Assert
					Assert.Equal(HttpStatusCode.OK, response.StatusCode);
					var responseString = await response.Content.ReadAsStringAsync();
					Assert.Contains(contentToCheck, responseString, StringComparison.InvariantCultureIgnoreCase);
				}
			}
		}

		[Fact]
		public async Task Page_Authenticated_ChangeProfile()
		{
			//logging-in
			// Arrange
			using var client = await _fixture.CreateAuthenticatedClientAsync();

			using (var request = new HttpRequestMessage(HttpMethod.Get, $"{client.BaseAddress}Identity/Account/Manage"))
			{
				// Act
				using (var get_response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead))
				{
					// Assert
					get_response.EnsureSuccessStatusCode();
					var antiforgery_token = await PostRequestHelper.ExtractAntiForgeryToken(get_response);

					// Arrange
					var payload = new Dictionary<string, string> {
						{ "__RequestVerificationToken", antiforgery_token },
						{ "Input.Name", "Alice Changed" },
						{ "Input.Email", "alice.testing@example.org" },
						{ "Input.PhoneNumber", "555438852" },
						{ "Input.DesktopNotifications", "true" },
					};

					using (var formPostBodyData = new FormUrlEncodedContent(payload))
					{
						PostRequestHelper.CreateFormUrlEncodedContentWithCookiesFromResponse(formPostBodyData.Headers, get_response);
						// Act
						using (var response = await client.PostAsync($"{client.BaseAddress}Identity/Account/Manage", formPostBodyData))
						{
							// Assert
							Assert.NotNull(response);
							response.EnsureSuccessStatusCode();

							var responseString = await response.Content.ReadAsStringAsync();
							Assert.Contains("Your profile has been updated", responseString);
							Assert.Contains("Alice Changed", responseString);
							Assert.Contains("alice.testing@example.org", responseString);
							Assert.Contains("555438852", responseString);
							Assert.Contains("name=\"Input.DesktopNotifications\" value=\"true\"", responseString);
						}
					}
				}//end using (var get_response
			}//end using request
		}

		[Fact]
		public async Task Page_Authenticated_ChangePassword()
		{
			//logging-in
			// Arrange
			using var client = await _fixture.CreateAuthenticatedClientAsync();

			using (var request = new HttpRequestMessage(HttpMethod.Get, $"{client.BaseAddress}Identity/Account/Manage/ChangePassword"))
			{
				// Act
				using (var get_response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead))
				{
					// Assert
					get_response.EnsureSuccessStatusCode();
					var antiforgery_token = await PostRequestHelper.ExtractAntiForgeryToken(get_response);

					// Arrange
					var payload = new Dictionary<string, string> {
						{ "__RequestVerificationToken", antiforgery_token },
						{ "Input.OldPassword", "#SecurePassword123" },
						{ "Input.NewPassword", "444changePassword&^%$" },
						{ "Input.ConfirmPassword", "444changePassword&^%$" }
					};

					using (var formPostBodyData = new FormUrlEncodedContent(payload))
					{
						PostRequestHelper.CreateFormUrlEncodedContentWithCookiesFromResponse(formPostBodyData.Headers, get_response);
						// Act
						using (var response = await client.PostAsync($"{client.BaseAddress}Identity/Account/Manage/ChangePassword", formPostBodyData))
						{
							// Assert
							Assert.NotNull(response);
							response.EnsureSuccessStatusCode();

							var responseString = await response.Content.ReadAsStringAsync();
							Assert.Contains("Your password has been changed.", responseString);
						}
					}
				}//end using (var get_response
			}//end using request
		}
	}
}
