namespace Santander.DevCodingTest.Api.Models;

public class StoryResponse
{
    public string Title { get; set; } = string.Empty;
    public string? Uri { get; set; }
    public string PostedBy { get; set; } = string.Empty;
    public DateTimeOffset Time { get; set; }
    public int Score { get; set; }
    public int CommentCount { get; set; }
}