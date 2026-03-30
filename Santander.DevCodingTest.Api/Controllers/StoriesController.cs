using Microsoft.AspNetCore.Mvc;
using Santander.DevCodingTest.Api.Models;
using Santander.DevCodingTest.Api.Services;

namespace Santander.DevCodingTest.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class StoriesController : ControllerBase
{
    private readonly IHackerNewsService _service;

    public StoriesController(IHackerNewsService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IEnumerable<StoryResponse>>> GetBestStories([FromQuery] int count)
    {
        if (count <= 0)
            return BadRequest("Count must be greater than zero.");

        try
        {
            var stories = await _service.GetBestStoriesAsync(count);
            return Ok(stories);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
    }
}