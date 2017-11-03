using EFGetStarted.AspNetCore.ExistingDb;
using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Integration
{
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
			using (HttpResponseMessage response = await _client.GetAsync("/"))
			{
				// Assert
				response.EnsureSuccessStatusCode();

				var responseString = await response.Content.ReadAsStringAsync();
				Assert.Contains("<title>Home Page - EFGetStarted.AspNetCore.ExistingDb</title>", responseString);
				Assert.Contains("<h2>Links</h2>", responseString);
			}
		}

		[Fact]
		public async Task ErrorHandlerTest()
		{
			// Arrange
			var data = new Dictionary<string, string>
			{
				{ "action", "exception" }
			};
			using (var content = new FormUrlEncodedContent(data))
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
			var data = new Dictionary<string, string>
			{
				{ "action", "exception" }
			};
			using (var content = new FormUrlEncodedContent(data))
			{
				// Act
				using (var response = await _client.PostAsync("/Home/UnintentionalErr", content))
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
			var data = new Dictionary<string, string>
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
			using (var content = new FormUrlEncodedContent(data))
			{
				// Act
				using (var response = await _client.PostAsync("/Home/ClientsideLog", content))
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
	public class HashesDataTablePage
	{
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
			using (HttpResponseMessage response = await _client.GetAsync("/HashesDataTable/"))
			{
				// Assert
				response.EnsureSuccessStatusCode();

				var responseString = await response.Content.ReadAsStringAsync();
				Assert.Contains("<button id=\"btninfo\" class=\"btn btn-default\" type=\"button\">" +
					"<i class=\"glyphicon glyphicon-info-sign\"></i>&nbsp;row info</button>",
					responseString);
				Assert.Contains("data-page-list=\"[5,10,20,50,500,2000]\"", responseString);
			}
		}

		[Fact]
		public async Task Load_Valid()
		{
			if (_fixture.DBKind == "sqlite") return;//pass on fake DB with no data

			// Arrange
			var data = new Dictionary<string, string>
			{
				{ "Sort", "Key" },
				{ "Order", "desc" },
				{ "Search", "kawa" },
				{ "Limit", 5.ToString() },
				{ "Offset", "1" }
			};
			using (var content = new FormUrlEncodedContent(data))
			{
				var queryString = await content.ReadAsStringAsync();
				// Act
				using (HttpResponseMessage response = await _client.GetAsync($"/HashesDataTable/Load?{queryString}", HttpCompletionOption.ResponseContentRead))
				{
					// Assert
					Assert.NotNull(response);
					response.EnsureSuccessStatusCode();
					Assert.Equal(HttpStatusCode.OK, response.StatusCode);

					var jsonString = await response.Content.ReadAsStringAsync();
					var typed_result = new
					{
						total = 1,
						rows = new ThinHashes[] { }
					};

					// Deserialize JSON String into concrete class
					var deserialized = JsonConvert.DeserializeObject(jsonString, typed_result.GetType()) as dynamic;
					Assert.IsType(typed_result.GetType(), deserialized);
					Assert.IsAssignableFrom<IEnumerable<ThinHashes>>(deserialized.rows);
					Assert.True(deserialized.rows.Length == 5);
					Assert.True(deserialized.total > 0);
					Assert.NotNull((deserialized.rows as ThinHashes[]).FirstOrDefault(r => r.Key.StartsWith("kawa")));
				}
			}
		}

		[Fact]
		public async Task Load_Invalid()
		{
			// Arrange
			var data = new Dictionary<string, string>
			{
				{ "Sort", "dead" },
				{ "Order", "string" },
				{ "Search", "is" },
				{ "Limit", 0xDEAD.ToString() },
				{ "Offset", "to the death" }
			};
			using (var content = new FormUrlEncodedContent(data))
			{
				var queryString = await content.ReadAsStringAsync();
				// Act
				using (HttpResponseMessage response = await _client.GetAsync($"/HashesDataTable/Load?{queryString}", HttpCompletionOption.ResponseContentRead))
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
}
