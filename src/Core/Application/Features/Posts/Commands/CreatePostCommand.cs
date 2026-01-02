using MediatR;
using Mootable.Application.Common.Responses;
using Mootable.Application.Features.Posts.DTOs;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using Mootable.Domain.Enums;

namespace Mootable.Application.Features.Posts.Commands;

/// <summary>
/// Command to create a new post in Wonderland
/// Matrix'ten çıkış: Düşüncelerini paylaş, gerçeği keşfet
/// </summary>
public class CreatePostCommand : IRequest<ServiceResponse<PostResponseDto>>
{
    public string Content { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> MediaUrls { get; set; } = new();
    public PostVisibility Visibility { get; set; } = PostVisibility.Public;
    public Guid? ParentPostId { get; set; }
}

public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, ServiceResponse<PostResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreatePostCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResponse<PostResponseDto>> Handle(
        CreatePostCommand request,
        CancellationToken cancellationToken)
    {
        // Kullanıcı kimlik kontrolü
        if (!_currentUserService.UserId.HasValue)
        {
            return ServiceResponse<PostResponseDto>.Failure("Unauthorized access");
        }

        var userId = _currentUserService.UserId.Value;

        // Kullanıcı kontrolü
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ServiceResponse<PostResponseDto>.Failure("User not found");
        }

        // Parent post kontrolü (eğer bu bir reply ise)
        if (request.ParentPostId.HasValue)
        {
            var parentPost = await _unitOfWork.Posts.GetByIdAsync(request.ParentPostId.Value, cancellationToken);
            if (parentPost == null)
            {
                return ServiceResponse<PostResponseDto>.Failure("Parent post not found");
            }
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

        // Yeni post oluştur
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Content = request.Content,
            HtmlContent = request.HtmlContent,
            Category = request.Category,
            Tags = request.Tags ?? new List<string>(),
            MediaUrls = request.MediaUrls ?? new List<string>(),
            Visibility = request.Visibility,
            ParentPostId = request.ParentPostId,
            UserId = userId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            EnlightenmentScore = 0, // Başlangıç puanı
            LikeCount = 0,
            ReplyCount = 0,
            ViewCount = 0,
            ShareCount = 0,
            ReportCount = 0,
            IsModerated = false,
            IsPinned = false
        };

        // Repository kullanarak ekle
        await _unitOfWork.Posts.AddAsync(post, cancellationToken);

        // Parent post'un reply count'unu artır
        if (request.ParentPostId.HasValue)
        {
            var parentPost = await _unitOfWork.Posts.GetByIdAsync(request.ParentPostId.Value, cancellationToken);
            if (parentPost != null)
            {
                parentPost.ReplyCount++;
                _unitOfWork.Posts.Update(parentPost);
            }
        }

        // Değişiklikleri kaydet
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            UserName = user.Username,
            UserDisplayName = user.DisplayName ?? user.Username,
            UserAvatarUrl = user.AvatarUrl,
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
            "Post created successfully. Welcome to the real world.");
    }
}