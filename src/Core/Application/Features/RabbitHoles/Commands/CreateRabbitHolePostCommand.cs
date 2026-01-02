using MediatR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using Mootable.Domain.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mootable.Application.Features.RabbitHoles.Commands
{
    /// <summary>
    /// Post a message in a Rabbit Hole
    /// Matrix theme: "Transmit deeper into the rabbit hole"
    /// </summary>
    public class CreateRabbitHolePostCommand : IRequest<CreateRabbitHolePostResponse>
    {
        public Guid RabbitHoleId { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid? ParentPostId { get; set; }
        public string? Tags { get; set; }
        public string? MediaUrls { get; set; } // JSON array of URLs
        public bool IsRedPill { get; set; } = false; // Paradigm-shifting insight
    }

    public class CreateRabbitHolePostResponse
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CreateRabbitHolePostCommandHandler : IRequestHandler<CreateRabbitHolePostCommand, CreateRabbitHolePostResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateRabbitHolePostCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CreateRabbitHolePostResponse> Handle(CreateRabbitHolePostCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

            // Verify rabbit hole exists
            var rabbitHole = await _context.RabbitHoles.FindAsync(request.RabbitHoleId);
            if (rabbitHole == null)
            {
                throw new InvalidOperationException("Rabbit hole not found in the Matrix");
            }

            // Check minimum enlightenment score requirement
            // Note: EnlightenmentScore feature disabled for now
            // var user = await _context.Users.FindAsync(userId);
            // if (user != null && user.EnlightenmentScore < rabbitHole.MinimumEnlightenmentScore)
            // {
            //     throw new InvalidOperationException($"Your enlightenment score is not sufficient to post in this rabbit hole");
            // }

            // Verify parent post if replying
            if (request.ParentPostId.HasValue)
            {
                var parentPost = await _context.RabbitHolePosts.FindAsync(request.ParentPostId.Value);
                if (parentPost == null || parentPost.RabbitHoleId != request.RabbitHoleId)
                {
                    throw new InvalidOperationException("Parent transmission not found in this rabbit hole");
                }
            }

            var post = new RabbitHolePost
            {
                Id = Guid.NewGuid(),
                Content = request.Content,
                RabbitHoleId = request.RabbitHoleId,
                AuthorId = userId,
                ParentPostId = request.ParentPostId,
                Tags = request.Tags ?? string.Empty,
                MediaUrls = request.MediaUrls,
                IsRedPill = request.IsRedPill,
                Visibility = PostVisibility.Public,
                CreatedBy = userId
            };

            _context.RabbitHolePosts.Add(post);

            // Update rabbit hole post count
            rabbitHole.PostCount++;

            // Award enlightenment score for posting
            // Note: EnlightenmentScore feature disabled for now
            // if (user != null)
            // {
            //     user.EnlightenmentScore += request.IsRedPill ? 5 : 1;
            // }

            await _context.SaveChangesAsync(cancellationToken);

            return new CreateRabbitHolePostResponse
            {
                Id = post.Id,
                CreatedAt = post.CreatedAt,
                Message = request.IsRedPill
                    ? "Red pill transmission sent. The Matrix will never be the same."
                    : $"Transmission sent deeper into the rabbit hole \"{rabbitHole.Name}\""
            };
        }
    }
}
