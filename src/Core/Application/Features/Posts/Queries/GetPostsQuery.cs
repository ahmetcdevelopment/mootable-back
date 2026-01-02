using MediatR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Common.Responses;
using Mootable.Application.Features.Posts.DTOs;
using Mootable.Application.Interfaces;
using Mootable.Domain.Enums;

namespace Mootable.Application.Features.Posts.Queries;

/// <summary>
/// Query to get posts for Wonderland feed
/// Matrix teması: Timeline'da gerçekliğin farklı katmanlarını keşfet
/// </summary>
public class GetPostsQuery : IRequest<ServiceResponse<GetPostsResponse>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Category { get; set; }
    public string? SearchTerm { get; set; }
    public List<string>? Tags { get; set; }
    public PostSortBy SortBy { get; set; } = PostSortBy.Latest;
    public bool IncludeReplies { get; set; } = false;
}

public enum PostSortBy
{
    Latest,
    Popular,
    MostLiked,
    MostReplied,
    Enlightened // En yüksek enlightenment score'a göre
}

public class GetPostsResponse
{
    public List<PostResponseDto> Posts { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class GetPostsQueryHandler : IRequestHandler<GetPostsQuery, ServiceResponse<GetPostsResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public GetPostsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async Task<ServiceResponse<GetPostsResponse>> Handle(
        GetPostsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        // Query oluştur
        var query = _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Where(p => !p.IsDeleted);

        // Reply filtrelemesi
        if (!request.IncludeReplies)
        {
            query = query.Where(p => p.ParentPostId == null);
        }

        // Kategori filtresi
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(p => p.Category == request.Category);
        }

        // Arama filtresi
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(p =>
                p.Content.ToLower().Contains(searchLower) ||
                (p.HtmlContent != null && p.HtmlContent.ToLower().Contains(searchLower)));
        }

        // Tag filtresi
        if (request.Tags != null && request.Tags.Any())
        {
            query = query.Where(p => p.Tags.Any(t => request.Tags.Contains(t)));
        }

        // Sıralama
        query = request.SortBy switch
        {
            PostSortBy.Popular => query.OrderByDescending(p => p.ViewCount)
                                       .ThenByDescending(p => p.CreatedAt),
            PostSortBy.MostLiked => query.OrderByDescending(p => p.LikeCount)
                                         .ThenByDescending(p => p.CreatedAt),
            PostSortBy.MostReplied => query.OrderByDescending(p => p.ReplyCount)
                                           .ThenByDescending(p => p.CreatedAt),
            PostSortBy.Enlightened => query.OrderByDescending(p => p.EnlightenmentScore)
                                           .ThenByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt) // Latest
        };

        // Toplam sayıyı al
        var totalCount = await query.CountAsync(cancellationToken);

        // Sayfalama
        var skip = (request.PageNumber - 1) * request.PageSize;
        var posts = await query
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // DTO'ya dönüştür
        var postDtos = new List<PostResponseDto>();
        foreach (var post in posts)
        {
            // View count'u artır (her görüntülemede)
            post.ViewCount++;
            _unitOfWork.Posts.Update(post);

            var dto = new PostResponseDto
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

            postDtos.Add(dto);
        }

        // View count güncellemelerini kaydet
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new GetPostsResponse
        {
            Posts = postDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            HasNextPage = (request.PageNumber * request.PageSize) < totalCount,
            HasPreviousPage = request.PageNumber > 1
        };

        return ServiceResponse<GetPostsResponse>.Success(
            response,
            "Welcome to Wonderland. Choose your reality.");
    }
}