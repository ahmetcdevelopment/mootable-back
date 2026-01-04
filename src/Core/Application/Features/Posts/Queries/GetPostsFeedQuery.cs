using MediatR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Common.Responses;
using Mootable.Application.Features.Posts.DTOs;
using Mootable.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mootable.Application.Features.Posts.Queries;

/// <summary>
/// Cursor-based pagination query for infinite scroll
/// Matrix theme: "There is no spoon" - seamless scrolling through reality
/// </summary>
public class GetPostsFeedQuery : IRequest<ServiceResponse<CursorPagedResult<PostResponseDto>>>
{
    /// <summary>
    /// Base64 encoded cursor (timestamp + id for tie-breaking)
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Number of items to fetch per page
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort direction: latest or oldest first
    /// </summary>
    public FeedDirection Direction { get; set; } = FeedDirection.Newer;

    /// <summary>
    /// Filter by category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Include only followed RabbitHoles posts
    /// </summary>
    public bool OnlyFollowing { get; set; } = false;
}

public enum FeedDirection
{
    Newer, // Get posts newer than cursor
    Older  // Get posts older than cursor (default for infinite scroll down)
}

/// <summary>
/// Response with cursor-based pagination metadata
/// </summary>
public class CursorPagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public string? NextCursor { get; set; }
    public string? PreviousCursor { get; set; }
    public bool HasMore { get; set; }
    public int TotalEstimatedCount { get; set; }
}

public class GetPostsFeedQueryHandler : IRequestHandler<GetPostsFeedQuery, ServiceResponse<CursorPagedResult<PostResponseDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public GetPostsFeedQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResponse<CursorPagedResult<PostResponseDto>>> Handle(
        GetPostsFeedQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        // Base query - only parent posts, not replies
        var query = _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Where(p => !p.IsDeleted && p.ParentPostId == null);

        // Category filter
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(p => p.Category == request.Category);
        }

        // Following filter (RabbitHoles the user follows)
        if (request.OnlyFollowing && userId.HasValue)
        {
            var followedRabbitHoleIds = await _context.RabbitHoleFollowers
                .Where(f => f.UserId == userId.Value)
                .Select(f => f.RabbitHoleId)
                .ToListAsync(cancellationToken);

            // Filter posts from followed users or with followed RabbitHole topic
            query = query.Where(p =>
                p.Category != null && followedRabbitHoleIds.Any(rh =>
                    p.Category.ToLower().Contains(rh.ToString().ToLower())));
        }

        // Decode cursor if provided
        DateTime? cursorDate = null;
        Guid? cursorId = null;
        if (!string.IsNullOrEmpty(request.Cursor))
        {
            try
            {
                var cursorBytes = Convert.FromBase64String(request.Cursor);
                var cursorString = Encoding.UTF8.GetString(cursorBytes);
                var parts = cursorString.Split('|');
                if (parts.Length == 2)
                {
                    cursorDate = DateTime.Parse(parts[0]);
                    cursorId = Guid.Parse(parts[1]);
                }
            }
            catch
            {
                // Invalid cursor, ignore
            }
        }

        // Apply cursor filter
        if (cursorDate.HasValue && cursorId.HasValue)
        {
            if (request.Direction == FeedDirection.Older)
            {
                // Get older posts (scrolling down)
                query = query.Where(p =>
                    p.CreatedAt < cursorDate.Value ||
                    (p.CreatedAt == cursorDate.Value && p.Id.CompareTo(cursorId.Value) < 0));
            }
            else
            {
                // Get newer posts (refreshing/polling)
                query = query.Where(p =>
                    p.CreatedAt > cursorDate.Value ||
                    (p.CreatedAt == cursorDate.Value && p.Id.CompareTo(cursorId.Value) > 0));
            }
        }

        // Order by created date, then by ID for stable pagination
        query = request.Direction == FeedDirection.Newer
            ? query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id)
            : query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);

        // Get one extra item to check if there are more
        var posts = await query
            .Take(request.PageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = posts.Count > request.PageSize;
        if (hasMore)
        {
            posts = posts.Take(request.PageSize).ToList();
        }

        // If we fetched in ascending order (newer), reverse for display
        if (request.Direction == FeedDirection.Newer)
        {
            posts.Reverse();
        }

        // Map to DTOs
        var postDtos = posts.Select(post => new PostResponseDto
        {
            Id = post.Id,
            Content = post.Content,
            HtmlContent = post.HtmlContent,
            Category = post.Category,
            Tags = post.Tags,
            MediaUrls = post.MediaUrls,
            Visibility = post.Visibility,
            ParentPostId = post.ParentPostId,
            UserId = post.UserId,
            UserName = post.User.Username,
            UserDisplayName = post.User.DisplayName ?? post.User.Username,
            UserAvatarUrl = post.User.AvatarUrl,
            LikeCount = post.LikeCount,
            ReplyCount = post.ReplyCount,
            ViewCount = post.ViewCount,
            ShareCount = post.ShareCount,
            EnlightenmentScore = post.EnlightenmentScore,
            IsLikedByCurrentUser = userId.HasValue && post.Likes.Any(l => l.UserId == userId.Value),
            IsOwnPost = userId.HasValue && post.UserId == userId.Value,
            IsPinned = post.IsPinned,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        }).ToList();

        // Generate cursors
        string? nextCursor = null;
        string? previousCursor = null;

        if (postDtos.Any())
        {
            // Next cursor (for loading older posts)
            var lastPost = postDtos.Last();
            var lastCursorData = $"{lastPost.CreatedAt:O}|{lastPost.Id}";
            nextCursor = Convert.ToBase64String(Encoding.UTF8.GetBytes(lastCursorData));

            // Previous cursor (for loading newer posts)
            var firstPost = postDtos.First();
            var firstCursorData = $"{firstPost.CreatedAt:O}|{firstPost.Id}";
            previousCursor = Convert.ToBase64String(Encoding.UTF8.GetBytes(firstCursorData));
        }

        // Estimate total count (cached or approximate)
        var totalEstimatedCount = await _context.Posts
            .CountAsync(p => !p.IsDeleted && p.ParentPostId == null, cancellationToken);

        var result = new CursorPagedResult<PostResponseDto>
        {
            Items = postDtos,
            NextCursor = hasMore ? nextCursor : null,
            PreviousCursor = previousCursor,
            HasMore = hasMore,
            TotalEstimatedCount = totalEstimatedCount
        };

        return ServiceResponse<CursorPagedResult<PostResponseDto>>.Success(
            result,
            "Follow the white rabbit...");
    }
}
