using MediatR;
using Mootable.Application.Common.Responses;
using Mootable.Application.Features.Posts.DTOs;
using Mootable.Application.Interfaces;

namespace Mootable.Application.Features.Posts.Commands;

/// <summary>
/// Command to update an existing post
/// Matrix'ten çıkış: Düşüncelerini yeniden şekillendir
/// </summary>
public class UpdatePostCommand : IRequest<ServiceResponse<PostResponseDto>>
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> MediaUrls { get; set; } = new();
}

public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, ServiceResponse<PostResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdatePostCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResponse<PostResponseDto>> Handle(
        UpdatePostCommand request,
        CancellationToken cancellationToken)
    {
        // Kullanıcı kimlik kontrolü
        if (!_currentUserService.UserId.HasValue)
        {
            return ServiceResponse<PostResponseDto>.Failure("Unauthorized access");
        }

        var userId = _currentUserService.UserId.Value;

        // Post'u getir
        var post = await _unitOfWork.Posts.GetByIdAsync(request.Id, cancellationToken);
        if (post == null)
        {
            return ServiceResponse<PostResponseDto>.Failure("Post not found");
        }

        // Sadece kendi postunu güncelleyebilir
        if (post.UserId != userId)
        {
            return ServiceResponse<PostResponseDto>.Failure("You can only update your own posts");
        }

        // Content validasyonu
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return ServiceResponse<PostResponseDto>.Failure("Post content cannot be empty");
        }

        if (request.Content.Length > 5000)
        {
            return ServiceResponse<PostResponseDto>.Failure("Post content exceeds maximum length of 5000 characters");
        }

        // Post'u güncelle
        post.Content = request.Content;
        post.HtmlContent = request.HtmlContent;
        post.Category = request.Category;
        post.Tags = request.Tags ?? new List<string>();
        post.MediaUrls = request.MediaUrls ?? new List<string>();
        post.UpdatedAt = DateTime.UtcNow;
        post.UpdatedBy = userId;

        // Repository kullanarak güncelle
        _unitOfWork.Posts.Update(post);

        // Değişiklikleri kaydet
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Kullanıcı bilgilerini getir
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

        // Response DTO'yu oluştur
        var response = new PostResponseDto
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
            UserName = user?.Username ?? "Unknown",
            UserDisplayName = user?.DisplayName ?? user?.Username ?? "Unknown",
            UserAvatarUrl = user?.AvatarUrl,
            LikeCount = post.LikeCount,
            ReplyCount = post.ReplyCount,
            ViewCount = post.ViewCount,
            ShareCount = post.ShareCount,
            EnlightenmentScore = post.EnlightenmentScore,
            IsLikedByCurrentUser = false,
            IsOwnPost = true,
            IsPinned = post.IsPinned,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        };

        return ServiceResponse<PostResponseDto>.Success(
            response,
            "Post updated successfully. Reality has been reshaped.");
    }
}