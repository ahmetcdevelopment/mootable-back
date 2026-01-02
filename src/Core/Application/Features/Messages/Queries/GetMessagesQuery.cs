using Mootable.Application.Common.Responses;
using Mootable.Application.Interfaces;
using Application.Features.Messages.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Messages.Queries
{
    public class GetMessagesQuery : IRequest<ServiceResponse<GetMessagesResponseDto>>
    {
        public Guid MootTableId { get; set; }
        public int Limit { get; set; } = 50;
        public Guid? Before { get; set; }
        public Guid? After { get; set; }
    }

    public class GetMessagesResponseDto
    {
        public List<MessageResponseDto> Messages { get; set; } = new();
        public bool HasMore { get; set; }
        public Guid? OldestMessageId { get; set; }
        public Guid? NewestMessageId { get; set; }
    }

    public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, ServiceResponse<GetMessagesResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetMessagesQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResponse<GetMessagesResponseDto>> Handle(
            GetMessagesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = _currentUserService.UserId
                    ?? throw new UnauthorizedAccessException("User not authenticated");

                // Verify MootTable exists and user has access
                var mootTable = await _unitOfWork.MootTables.GetQueryableWithIncludes(
                        mt => mt.Server,
                        mt => mt.Server.Members)
                    .FirstOrDefaultAsync(mt => mt.Id == request.MootTableId, cancellationToken);

                if (mootTable == null)
                {
                    return ServiceResponse<GetMessagesResponseDto>.Failure(
                        "Transmission deck not found.");
                }

                // Check if user is a member of the server
                var isMember = mootTable.Server.Members.Any(m => m.UserId == currentUserId);
                if (!isMember)
                {
                    return ServiceResponse<GetMessagesResponseDto>.Failure(
                        "Access denied. Not authorized to access this deck.");
                }

                // Build query
                var query = _unitOfWork.Messages.GetQueryableWithIncludes(
                        m => m.Author,
                        m => m.Reactions)
                    .Where(m => m.MootTableId == request.MootTableId);

                // Apply pagination
                if (request.Before.HasValue)
                {
                    var beforeMessage = await _unitOfWork.Messages
                        .GetByIdAsync(request.Before.Value, cancellationToken);
                    if (beforeMessage != null)
                    {
                        query = query.Where(m => m.CreatedAt < beforeMessage.CreatedAt);
                    }
                }

                if (request.After.HasValue)
                {
                    var afterMessage = await _unitOfWork.Messages
                        .GetByIdAsync(request.After.Value, cancellationToken);
                    if (afterMessage != null)
                    {
                        query = query.Where(m => m.CreatedAt > afterMessage.CreatedAt);
                    }
                }

                // Get messages
                var messages = await query
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(request.Limit + 1) // Take one extra to check if there's more
                    .ToListAsync(cancellationToken);

                var hasMore = messages.Count > request.Limit;
                if (hasMore)
                {
                    messages = messages.Take(request.Limit).ToList();
                }

                // Map to DTOs
                var messageDtos = messages.Select(m => new MessageResponseDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    AuthorId = m.AuthorId,
                    AuthorUsername = m.Author.Username,
                    AuthorAvatarUrl = m.Author.AvatarUrl,
                    MootTableId = m.MootTableId,
                    ReplyToId = m.ReplyToId,
                    ReplyTo = m.ReplyTo != null ? new MessageResponseDto
                    {
                        Id = m.ReplyTo.Id,
                        Content = m.ReplyTo.Content,
                        AuthorId = m.ReplyTo.AuthorId,
                        AuthorUsername = m.ReplyTo.Author?.Username ?? "Unknown",
                        CreatedAt = m.ReplyTo.CreatedAt
                    } : null,
                    Type = m.Type,
                    IsEdited = m.IsEdited,
                    IsPinned = m.IsPinned,
                    CreatedAt = m.CreatedAt,
                    ReactionCount = m.Reactions.Count
                }).ToList();

                // Reverse to get chronological order
                messageDtos.Reverse();

                var response = new GetMessagesResponseDto
                {
                    Messages = messageDtos,
                    HasMore = hasMore,
                    OldestMessageId = messageDtos.FirstOrDefault()?.Id,
                    NewestMessageId = messageDtos.LastOrDefault()?.Id
                };

                return ServiceResponse<GetMessagesResponseDto>.Success(response,
                    "Transmissions retrieved from the Matrix.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<GetMessagesResponseDto>.Failure(
                    $"Failed to retrieve transmissions: {ex.Message}");
            }
        }
    }
}