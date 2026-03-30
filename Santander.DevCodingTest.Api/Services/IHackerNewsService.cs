using Santander.DevCodingTest.Api.Models;

namespace Santander.DevCodingTest.Api.Services;

public interface IHackerNewsService
{
    Task<IEnumerable<StoryResponse>> GetBestStoriesAsync(int count);
}