using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Santander.DevCodingTest.Api.Models;
using Santander.DevCodingTest.Api.Services;

namespace Santander.DevCodingTest.Tests.Unit;

public class HackerNewsServiceTests
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    private const string BaseAddress = "https://hacker-news.firebaseio.com/v0/";

    public HackerNewsServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HackerNews:CacheExpirationMinutes"] = "5"
            })
            .Build();
    }

    private HackerNewsService CreateService(HttpClient httpClient)
        => new(httpClient, _cache, _configuration);

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
        => new(handler) { BaseAddress = new Uri(BaseAddress) };

    private static HttpClient CreateHttpClientWithResponses(
        int[] bestStoryIds,
        Dictionary<int, HackerNewsItem> stories,
        Action? onCall = null)
    {
        var handler = new MockHttpMessageHandler(request =>
        {
            var path = request.RequestUri!.PathAndQuery;

            if (path.Contains("beststories"))
            {
                onCall?.Invoke();
                var json = JsonSerializer.Serialize(bestStoryIds);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                };
            }

            foreach (var (id, item) in stories)
            {
                if (path.Contains($"item/{id}"))
                {
                    var json = JsonSerializer.Serialize(item);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json)
                    };
                }
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        return CreateHttpClient(handler);
    }

    private static HttpClient CreateTimeoutHttpClient()
        => CreateHttpClient(new MockHttpMessageHandler(simulateTimeout: true));

    [Fact]
    public async Task GetBestStoriesAsync_WhenHackerNewsApiResponds_ReturnsStoriesOrderedByScoreDescending()
    {
        // Arrange
        var bestStoryIds = new[] { 1, 2, 3 };
        var stories = new Dictionary<int, HackerNewsItem>
        {
            [1] = new HackerNewsItem { Id = 1, Title = "Story Low",    Score = 100, By = "user1", Time = 1000, Descendants = 10 },
            [2] = new HackerNewsItem { Id = 2, Title = "Story High",   Score = 300, By = "user2", Time = 2000, Descendants = 20 },
            [3] = new HackerNewsItem { Id = 3, Title = "Story Medium", Score = 200, By = "user3", Time = 3000, Descendants = 30 },
        };

        var service = CreateService(CreateHttpClientWithResponses(bestStoryIds, stories));

        // Act
        var result = await service.GetBestStoriesAsync(3);

        // Assert
        var list = result.ToList();
        list.Should().HaveCount(3);
        list[0].Score.Should().Be(300);
        list[1].Score.Should().Be(200);
        list[2].Score.Should().Be(100);
    }

    [Fact]
    public async Task GetBestStoriesAsync_WhenCalledTwice_ShouldHitCacheOnSecondCall()
    {
        // Arrange
        var bestStoryIds = new[] { 1, 2 };
        var stories = new Dictionary<int, HackerNewsItem>
        {
            [1] = new HackerNewsItem { Id = 1, Title = "Story A", Score = 100, By = "user1", Time = 1000, Descendants = 5 },
            [2] = new HackerNewsItem { Id = 2, Title = "Story B", Score = 200, By = "user2", Time = 2000, Descendants = 10 },
        };

        var callCount = 0;
        var service = CreateService(CreateHttpClientWithResponses(bestStoryIds, stories, onCall: () => callCount++));

        // Act
        await service.GetBestStoriesAsync(2);
        await service.GetBestStoriesAsync(2);

        // Assert
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBestStoriesAsync_WhenCountExceedsTotalStories_ReturnsAllAvailableStories()
    {
        // Arrange
        var bestStoryIds = new[] { 1, 2 };
        var stories = new Dictionary<int, HackerNewsItem>
        {
            [1] = new HackerNewsItem { Id = 1, Title = "Story A", Score = 100, By = "user1", Time = 1000, Descendants = 5 },
            [2] = new HackerNewsItem { Id = 2, Title = "Story B", Score = 200, By = "user2", Time = 2000, Descendants = 10 },
        };

        var service = CreateService(CreateHttpClientWithResponses(bestStoryIds, stories));

        // Act
        var result = await service.GetBestStoriesAsync(999);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBestStoriesAsync_WhenApiTimesOut_ShouldReturnCachedStories()
    {
        // Arrange
        var cachedStories = new List<StoryResponse>
        {
            new() { Title = "Cached Story", Score = 500, PostedBy = "user1", Time = DateTimeOffset.UtcNow, CommentCount = 10 }
        };

        _cache.Set("best_stories_fallback", cachedStories, DateTimeOffset.MaxValue);

        var service = CreateService(CreateTimeoutHttpClient());

        // Act
        var result = await service.GetBestStoriesAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Cached Story");
    }

    [Fact]
    public async Task GetBestStoriesAsync_WhenApiTimesOutAndCacheIsEmpty_ShouldThrowException()
    {
        // Arrange
        var service = CreateService(CreateTimeoutHttpClient());

        // Act
        var act = async () => await service.GetBestStoriesAsync(5);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unavailable*");
    }
}