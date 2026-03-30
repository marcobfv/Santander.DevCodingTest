using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Santander.DevCodingTest.Api.Controllers;
using Santander.DevCodingTest.Api.Models;
using Santander.DevCodingTest.Api.Services;

namespace Santander.DevCodingTest.Tests.Unit;

public class StoriesControllerTests
{
    private readonly IHackerNewsService _service;
    private readonly StoriesController _controller;

    public StoriesControllerTests()
    {
        _service = Substitute.For<IHackerNewsService>();
        _controller = new StoriesController(_service);
    }

    [Fact]
    public async Task GetBestStories_WhenValidCount_ReturnsOkWithStories()
    {
        // Arrange
        var stories = new List<StoryResponse>
        {
            new() { Title = "Story 1", Score = 300, PostedBy = "user1", Time = DateTimeOffset.UtcNow, CommentCount = 10 },
            new() { Title = "Story 2", Score = 200, PostedBy = "user2", Time = DateTimeOffset.UtcNow, CommentCount = 20 },
        };

        _service.GetBestStoriesAsync(2).Returns(stories);

        // Act
        var actionResult = await _controller.GetBestStories(2);

        // Assert
        var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeAssignableTo<IEnumerable<StoryResponse>>().Subject;
        body.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetBestStories_WhenInvalidCount_ReturnsBadRequest(int count)
    {
        // Act
        var actionResult = await _controller.GetBestStories(count);

        // Assert
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetBestStories_WhenServiceThrowsInvalidOperationException_ReturnsServiceUnavailable()
    {
        // Arrange
        _service.GetBestStoriesAsync(5).ThrowsAsync(new InvalidOperationException("unavailable"));

        // Act
        var actionResult = await _controller.GetBestStories(5);

        // Assert
        var status = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        status.StatusCode.Should().Be(503);
    }
}