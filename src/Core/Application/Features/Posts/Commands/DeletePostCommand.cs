using MediatR;
using Mootable.Application.Common.Responses;
using Mootable.Application.Interfaces;

namespace Mootable.Application.Features.Posts.Commands;

/// <summary>
/// Command to delete a post (soft delete)
/// Matrix'ten çıkış: Bazı düşünceler silinmeli, bazıları sonsuza dek kalmalı
/// </summary>
public class DeletePostCommand : IRequest<ServiceResponse<bool>>
{
    public Guid Id { get; set; }
}

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, ServiceResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeletePostCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResponse<bool>> Handle(
        DeletePostCommand request,
        CancellationToken cancellationToken)
    {
        // Kullanıcı kimlik kontrolü
        if (!_currentUserService.UserId.HasValue)
        {
            return ServiceResponse<bool>.Failure("Unauthorized access");
        }

        var userId = _currentUserService.UserId.Value;

        // Post'u getir (includes ile replies'ları da getir)
        var post = await _unitOfWork.Posts.GetByIdAsync(
            request.Id,
            p => p.Replies);

        if (post == null)
        {
            return ServiceResponse<bool>.Failure("Post not found");
        }

        // Sadece kendi postunu silebilir
        if (post.UserId != userId)
        {
            return ServiceResponse<bool>.Failure("You can only delete your own posts");
        }

        // Post'a ait like'ları sil
        var postLikes = await _unitOfWork.PostLikes.GetAllAsync(
            pl => pl.PostId == request.Id,
            cancellationToken);

        if (postLikes.Any())
        {
            _unitOfWork.PostLikes.DeleteRange(postLikes);
        }

        // Parent post'un reply count'unu azalt
        if (post.ParentPostId.HasValue)
        {
            var parentPost = await _unitOfWork.Posts.GetByIdAsync(post.ParentPostId.Value, cancellationToken);
            if (parentPost != null)
            {
                parentPost.ReplyCount = Math.Max(0, parentPost.ReplyCount - 1);
                _unitOfWork.Posts.Update(parentPost);
            }
        }

        // Soft delete kullan
        await _unitOfWork.Posts.SoftDeleteAsync(post, cancellationToken);

        // Reply'ları da soft delete yap (cascade mantığı)
        if (post.Replies != null && post.Replies.Any())
        {
            foreach (var reply in post.Replies)
            {
                await _unitOfWork.Posts.SoftDeleteAsync(reply.Id, cancellationToken);
            }
        }

        // Değişiklikleri kaydet
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResponse<bool>.Success(
            true,
            "Post deleted successfully. Some truths are better forgotten.");
    }
}