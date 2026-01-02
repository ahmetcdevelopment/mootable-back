using MediatR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Common.Responses;
using Mootable.Application.Features.Posts.DTOs;
using Mootable.Application.Interfaces;

namespace Mootable.Application.Features.Posts.Queries;

/// <summary>
/// Query to get a single post by ID with replies
/// Matrix teması: Gerçeğin derinliklerine in
/// </summary>
public class GetPostByIdQuery : IRequest<ServiceResponse<PostDetailDto>>
{
    public Guid Id { get; set; }
    public bool IncludeReplies { get; set; } = true;
}

public class PostDetailDto : PostResponseDto
{
    public List<PostResponseDto> Replies { get; set; } = new();
    public PostResponseDto? ParentPostDetails { get; set; }
}

public class GetPostByIdQueryHandler : IRequestHandler<GetPostByIdQuery, ServiceResponse<PostDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public GetPostByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResponse<PostDetailDto>> Handle(
        GetPostByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        // Ana post'u getir
        var post = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.ParentPost)
                .ThenInclude(pp => pp!.User)
            .FirstOrDefaultAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken);

        if (post == null)
        {
            return ServiceResponse<PostDetailDto>.Failure("Post not found in the Matrix");
        }

        // View count'u artır
        post.ViewCount++;
        _unitOfWork.Posts.Update(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Ana post DTO'sunu oluştur
        var postDetail = new PostDetailDto
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
        };

        // Parent post detaylarını ekle
        if (post.ParentPost != null)
        {
            postDetail.ParentPostDetails = new PostResponseDto
            {
                Id = post.ParentPost.Id,
                Content = post.ParentPost.Content.Length > 200
                    ? post.ParentPost.Content.Substring(0, 200) + "..."
                    : post.ParentPost.Content,
                UserId = post.ParentPost.UserId,
                UserName = post.ParentPost.User.Username,
                UserDisplayName = post.ParentPost.User.DisplayName ?? post.ParentPost.User.Username,
                UserAvatarUrl = post.ParentPost.User.AvatarUrl,
                CreatedAt = post.ParentPost.CreatedAt
            };
        }

        // Reply'ları getir
        if (request.IncludeReplies)
        {
            var replies = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Where(p => p.ParentPostId == request.Id && !p.IsDeleted)
                .OrderByDescending(p => p.EnlightenmentScore) // En enlightened reply'lar önce
                .ThenByDescending(p => p.CreatedAt)
                .Take(50) // İlk 50 reply
                .ToListAsync(cancellationToken);

            postDetail.Replies = replies.Select(reply => new PostResponseDto
            {
                Id = reply.Id,
                Content = reply.Content,
                HtmlContent = reply.HtmlContent,
                Category = reply.Category,
                Tags = reply.Tags,
                MediaUrls = reply.MediaUrls,
                Visibility = reply.Visibility,
                ParentPostId = reply.ParentPostId,
                UserId = reply.UserId,
                UserName = reply.User.Username,
                UserDisplayName = reply.User.DisplayName ?? reply.User.Username,
                UserAvatarUrl = reply.User.AvatarUrl,
                LikeCount = reply.LikeCount,
                ReplyCount = reply.ReplyCount,
                ViewCount = reply.ViewCount,
                ShareCount = reply.ShareCount,
                EnlightenmentScore = reply.EnlightenmentScore,
                IsLikedByCurrentUser = userId.HasValue && reply.Likes.Any(l => l.UserId == userId.Value),
                IsOwnPost = userId.HasValue && reply.UserId == userId.Value,
                IsPinned = reply.IsPinned,
                CreatedAt = reply.CreatedAt,
                UpdatedAt = reply.UpdatedAt
            }).ToList();
        }

        return ServiceResponse<PostDetailDto>.Success(
            postDetail,
            "The rabbit hole goes deeper than you think.");
    }
}