using MediatR;
using Mootable.Application.Common.Responses;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;

namespace Mootable.Application.Features.Posts.Commands;

/// <summary>
/// Command to like or unlike a post
/// Matrix temasında: Red Pill (beğeni) veya Blue Pill (beğeniyi geri al)
/// </summary>
public class LikePostCommand : IRequest<ServiceResponse<LikePostResponse>>
{
    public Guid PostId { get; set; }
    public string LikeType { get; set; } = "RedPill"; // RedPill, BluePill, Awakened, Enlightened
}

public class LikePostResponse
{
    public bool IsLiked { get; set; }
    public int LikeCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class LikePostCommandHandler : IRequestHandler<LikePostCommand, ServiceResponse<LikePostResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public LikePostCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResponse<LikePostResponse>> Handle(
        LikePostCommand request,
        CancellationToken cancellationToken)
    {
        // Kullanıcı kimlik kontrolü
        if (!_currentUserService.UserId.HasValue)
        {
            return ServiceResponse<LikePostResponse>.Failure("Unauthorized access");
        }

        var userId = _currentUserService.UserId.Value;

        // Post'u kontrol et
        var post = await _unitOfWork.Posts.GetByIdAsync(request.PostId, cancellationToken);
        if (post == null)
        {
            return ServiceResponse<LikePostResponse>.Failure("Post not found");
        }

        // Mevcut like'ı kontrol et
        var existingLike = await _unitOfWork.PostLikes.FirstOrDefaultAsync(
            pl => pl.PostId == request.PostId && pl.UserId == userId,
            cancellationToken);

        bool isLiked;
        string message;

        if (existingLike != null)
        {
            // Like varsa kaldır (toggle mantığı)
            _unitOfWork.PostLikes.Delete(existingLike);
            post.LikeCount = Math.Max(0, post.LikeCount - 1);

            // Enlightenment score'u azalt
            post.EnlightenmentScore = Math.Max(0, post.EnlightenmentScore - 1);

            isLiked = false;
            message = "You've taken the blue pill. Reality unchanged.";
        }
        else
        {
            // Yeni like ekle
            var newLike = new PostLike
            {
                Id = Guid.NewGuid(),
                PostId = request.PostId,
                UserId = userId,
                LikeType = request.LikeType,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.PostLikes.AddAsync(newLike, cancellationToken);
            post.LikeCount++;

            // Enlightenment score'u artır (Matrix'ten çıkış seviyesi)
            post.EnlightenmentScore += GetEnlightenmentPoints(request.LikeType);

            isLiked = true;
            message = GetLikeMessage(request.LikeType);
        }

        // Post'u güncelle
        _unitOfWork.Posts.Update(post);

        // Değişiklikleri kaydet
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new LikePostResponse
        {
            IsLiked = isLiked,
            LikeCount = post.LikeCount,
            Message = message
        };

        return ServiceResponse<LikePostResponse>.Success(response);
    }

    private int GetEnlightenmentPoints(string likeType)
    {
        return likeType switch
        {
            "RedPill" => 1,
            "Awakened" => 2,
            "Enlightened" => 3,
            _ => 1
        };
    }

    private string GetLikeMessage(string likeType)
    {
        return likeType switch
        {
            "RedPill" => "You've taken the red pill. Welcome to the real world.",
            "BluePill" => "The blue pill keeps you in wonderland.",
            "Awakened" => "You're awakening to the truth.",
            "Enlightened" => "You've achieved enlightenment. The Matrix has no power here.",
            _ => "Your choice has been registered."
        };
    }
}