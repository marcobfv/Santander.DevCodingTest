using Microsoft.Extensions.Caching.Memory;
using Santander.DevCodingTest.Api.Models;

namespace Santander.DevCodingTest.Api.Services;

public class HackerNewsService : IHackerNewsService
{
    private const string BestStoriesCacheKey = "best_stories";
    private const string FallbackCacheKey = "best_stories_fallback";

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration;
    private readonly TimeSpan _fallbackCacheExpiration;

    public HackerNewsService(HttpClient httpClient, IMemoryCache cache, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _cache = cache;

        var minutes = int.Parse(configuration["HackerNews:CacheExpirationMinutes"]!);
        _cacheExpiration = TimeSpan.FromMinutes(minutes);
        _fallbackCacheExpiration = TimeSpan.FromMinutes(525600);
    }

    public async Task<IEnumerable<StoryResponse>> GetBestStoriesAsync(int count)
    {
        var stories = await FetchStoriesAsync();

        return stories
            .OrderByDescending(s => s.Score)
            .Take(count);
    }

    private async Task<IEnumerable<StoryResponse>> FetchStoriesAsync()
    {
        if (_cache.TryGetValue(BestStoriesCacheKey, out IEnumerable<StoryResponse>? cached) && cached is not null)
            return cached;

        try
        {
            var ids = await _httpClient.GetFromJsonAsync<int[]>("beststories.json");

            if (ids is null)
                throw new InvalidOperationException("Failed to retrieve best story IDs from Hacker News API.");

            var tasks = ids.Select(id => FetchStoryAsync(id));
            var stories = await Task.WhenAll(tasks);

            var validStories = stories
                .Where(s => s is not null)
                .Cast<StoryResponse>()
                .ToList();

            _cache.Set(BestStoriesCacheKey, validStories, _cacheExpiration);
            _cache.Set(FallbackCacheKey, validStories, _fallbackCacheExpiration);

            return validStories;
        }
        catch (TaskCanceledException)
        {
            if (_cache.TryGetValue(FallbackCacheKey, out IEnumerable<StoryResponse>? fallback) && fallback is not null)
                return fallback;

            throw new InvalidOperationException("Hacker News API is unavailable and no cached data is available.");
        }

    }

    private async Task<StoryResponse?> FetchStoryAsync(int id)
    {
        var item = await _httpClient.GetFromJsonAsync<HackerNewsItem>($"item/{id}.json");

        if (item is null)
            return null;

        return new StoryResponse
        {
            Title = item.Title,
            Uri = item.Url,
            PostedBy = item.By,
            Time = DateTimeOffset.FromUnixTimeSeconds(item.Time),
            Score = item.Score,
            CommentCount = item.Descendants
        };
    }
}