using Mootable.Domain.Enums;

namespace Mootable.Application.Features.Posts.DTOs;

public class PostResponseDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> MediaUrls { get; set; } = new();
    public PostVisibility Visibility { get; set; }

    // Parent post info
    public Guid? ParentPostId { get; set; }
    public PostResponseDto? ParentPost { get; set; }

    // User info
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }

    // Statistics
    public int LikeCount { get; set; }
    public int ReplyCount { get; set; }
    public int ViewCount { get; set; }
    public int ShareCount { get; set; }
    public int EnlightenmentScore { get; set; }

    // Interaction states
    public bool IsLikedByCurrentUser { get; set; }
    public bool IsOwnPost { get; set; }
    public bool IsPinned { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}