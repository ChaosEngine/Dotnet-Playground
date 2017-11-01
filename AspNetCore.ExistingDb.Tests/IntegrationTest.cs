using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Integration
{
	public class HomeController : IClassFixture<TestFixture<EFGetStarted.AspNetCore.ExistingDb.Startup>>
	{
		private readonly HttpClient _client;

		public HomeController(TestFixture<EFGetStarted.AspNetCore.ExistingDb.Startup> fixture)
		{
			_client = fixture.Client;
		}

		[Fact]
		public async Task HomePage()
		{
			// Arrange
			// Act
			HttpResponseMessage response = await _client.GetAsync("/");

			// Assert
			response.EnsureSuccessStatusCode();

			var responseString = await response.Content.ReadAsStringAsync();
			Assert.Contains("<title>Home Page - EFGetStarted.AspNetCore.ExistingDb</title>", responseString);
			Assert.Contains("<h2>Links</h2>", responseString);
		}

		[Fact]
		public async Task ErrorHandlerTest()
		{
			// Arrange
			var data = new Dictionary<string, string>
			{
				{ "action", "exception" }
			};
			var content = new FormUrlEncodedContent(data);


			// Act
			var response = await _client.PostAsync("/DeesNotExist/FooBar", content);

			// Assert
			Assert.NotNull(response);
			Assert.False(response.IsSuccessStatusCode);
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);


			// Act
			response = await _client.PostAsync("/Home/UnintentionalErr", content);

			// Assert
			Assert.NotNull(response);
			Assert.False(response.IsSuccessStatusCode);
			Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
			var responseString = await response.Content.ReadAsStringAsync();
			Assert.Contains("Exception: test exception", responseString);
		}
	}
}
