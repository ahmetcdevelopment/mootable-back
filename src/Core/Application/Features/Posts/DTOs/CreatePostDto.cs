using Mootable.Domain.Enums;

namespace Mootable.Application.Features.Posts.DTOs;

public class CreatePostDto
{
    public string Content { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> MediaUrls { get; set; } = new();
    public PostVisibility Visibility { get; set; } = PostVisibility.Public;
    public Guid? ParentPostId { get; set; }
}