using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Santander.DevCodingTest.Api.Models;
using Santander.DevCodingTest.Api.Services;

namespace Santander.DevCodingTest.Tests.Integration;

public class StoriesIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StoriesIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithService(IHackerNewsService service)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => service);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GET_Stories_WhenValidCount_Returns200WithCorrectSchema()
    {
        // Arrange
        var stories = new List<StoryResponse>
        {
            new() {
                Title       = "Story 1",
                Uri         = "https://example.com/1",
                PostedBy    = "user1",
                Time        = DateTimeOffset.UtcNow,
                Score       = 300,
                CommentCount = 10
            },
            new() {
                Title       = "Story 2",
                Uri         = "https://example.com/2",
                PostedBy    = "user2",
                Time        = DateTimeOffset.UtcNow,
                Score       = 200,
                CommentCount = 20
            }
        };

        var service = Substitute.For<IHackerNewsService>();
        service.GetBestStoriesAsync(2).Returns(stories);

        var client = CreateClientWithService(service);

        // Act
        var response = await client.GetAsync("/stories?count=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<List<StoryResponse>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(2);
        body[0].Title.Should().Be("Story 1");
        body[1].Title.Should().Be("Story 2");
    }

    [Theory]
    [InlineData("/stories?count=0")]
    [InlineData("/stories?count=-1")]
    [InlineData("/stories")]
    public async Task GET_Stories_WhenInvalidOrMissingCount_Returns400(string url)
    {
        // Arrange
        var service = Substitute.For<IHackerNewsService>();
        var client = CreateClientWithService(service);

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Stories_WhenServiceUnavailable_Returns503()
    {
        // Arrange
        var service = Substitute.For<IHackerNewsService>();
        service.GetBestStoriesAsync(5).ThrowsAsync(new InvalidOperationException("unavailable"));

        var client = CreateClientWithService(service);

        // Act
        var response = await client.GetAsync("/stories?count=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}