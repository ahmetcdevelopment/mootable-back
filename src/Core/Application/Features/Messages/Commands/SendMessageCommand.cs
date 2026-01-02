using Mootable.Application.Common.Responses;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Messages.Commands
{
    public class SendMessageCommand : IRequest<ServiceResponse<MessageResponseDto>>
    {
        public Guid MootTableId { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid? ReplyToId { get; set; }
        public MessageType Type { get; set; } = MessageType.Default;
    }

    public class MessageResponseDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid AuthorId { get; set; }
        public string AuthorUsername { get; set; } = string.Empty;
        public string? AuthorAvatarUrl { get; set; }
        public Guid? MootTableId { get; set; }
        public Guid? RabbitHoleId { get; set; }
        public Guid? ReplyToId { get; set; }
        public MessageResponseDto? ReplyTo { get; set; }
        public MessageType Type { get; set; }
        public bool IsEdited { get; set; }
        public bool IsPinned { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ReactionCount { get; set; }
    }

    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ServiceResponse<MessageResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public SendMessageCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResponse<MessageResponseDto>> Handle(
            SendMessageCommand request,
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
                    return ServiceResponse<MessageResponseDto>.Failure(
                        "Transmission deck not found. Check your coordinates.");
                }

                // Check if user is a member of the server
                var isMember = mootTable.Server.Members.Any(m => m.UserId == currentUserId);
                if (!isMember)
                {
                    return ServiceResponse<MessageResponseDto>.Failure(
                        "Access denied. You are not part of this ship's crew.");
                }

                // Handle reply
                MessageResponseDto? replyToDto = null;
                if (request.ReplyToId.HasValue)
                {
                    var replyTo = await _unitOfWork.Messages.GetQueryableWithIncludes(m => m.Author)
                        .FirstOrDefaultAsync(m => m.Id == request.ReplyToId.Value, cancellationToken);

                    if (replyTo != null)
                    {
                        replyToDto = new MessageResponseDto
                        {
                            Id = replyTo.Id,
                            Content = replyTo.Content,
                            AuthorId = replyTo.AuthorId,
                            AuthorUsername = replyTo.Author.Username,
                            CreatedAt = replyTo.CreatedAt
                        };
                    }
                }

                // Create message
                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    Content = request.Content,
                    AuthorId = currentUserId,
                    MootTableId = request.MootTableId,
                    ReplyToId = request.ReplyToId,
                    Type = request.Type,
                    CreatedBy = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Messages.AddAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Get the created message with author info
                var createdMessage = await _unitOfWork.Messages.GetQueryableWithIncludes(m => m.Author)
                    .FirstOrDefaultAsync(m => m.Id == message.Id, cancellationToken);

                var response = new MessageResponseDto
                {
                    Id = createdMessage!.Id,
                    Content = createdMessage.Content,
                    AuthorId = createdMessage.AuthorId,
                    AuthorUsername = createdMessage.Author.Username,
                    AuthorAvatarUrl = createdMessage.Author.AvatarUrl,
                    MootTableId = createdMessage.MootTableId,
                    ReplyToId = createdMessage.ReplyToId,
                    ReplyTo = replyToDto,
                    Type = createdMessage.Type,
                    IsEdited = createdMessage.IsEdited,
                    IsPinned = createdMessage.IsPinned,
                    CreatedAt = createdMessage.CreatedAt,
                    ReactionCount = 0
                };

                // Matrix-themed response messages
                var matrixMessages = new[]
                {
                    "Transmission sent through the Matrix.",
                    "Your message echoes through the construct.",
                    "Data packet transmitted successfully.",
                    "Signal broadcasted to the crew."
                };

                var randomMessage = matrixMessages[new Random().Next(matrixMessages.Length)];

                return ServiceResponse<MessageResponseDto>.Success(response, randomMessage);
            }
            catch (Exception ex)
            {
                return ServiceResponse<MessageResponseDto>.Failure(
                    $"Transmission failed. Matrix interference: {ex.Message}");
            }
        }
    }
}