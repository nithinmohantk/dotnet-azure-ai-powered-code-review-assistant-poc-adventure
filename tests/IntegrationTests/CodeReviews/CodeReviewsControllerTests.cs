// tests/IntegrationTests/CodeReviewAssistant.IntegrationTests/CodeReviews/CodeReviewsControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CodeReviewAssistant.IntegrationTests.CodeReviews
{
    public class CodeReviewsControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public CodeReviewsControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task GetCodeReviews_ReturnsSuccessAndCorrectContentType()
        {
            // Act
            var response = await _client.GetAsync("/api/codereviews");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", 
                response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task HealthCheck_ReturnsHealthy()
        {
            // Act
            var response = await _client.GetAsync("/api/healthcheck");
            
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Healthy", content);
        }
    }
}