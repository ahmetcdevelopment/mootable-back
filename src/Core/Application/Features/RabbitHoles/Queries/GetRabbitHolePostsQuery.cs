using MediatR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mootable.Application.Features.RabbitHoles.Queries
{
    /// <summary>
    /// Get posts from a Rabbit Hole
    /// Matrix theme: "Read transmissions from the rabbit hole"
    /// </summary>
    public class GetRabbitHolePostsQuery : IRequest<GetRabbitHolePostsResponse>
    {
        public Guid RabbitHoleId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "newest"; // newest, depth, truth
    }

    public class GetRabbitHolePostsResponse
    {
        public List<RabbitHolePostDto> Posts { get; set; } = new List<RabbitHolePostDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string RabbitHoleName { get; set; } = string.Empty;
    }

    public class RabbitHolePostDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid AuthorId { get; set; }
        public string AuthorUsername { get; set; } = string.Empty;
        public string AuthorAvatarUrl { get; set; } = string.Empty;
        public Guid? ParentPostId { get; set; }
        public int DepthScore { get; set; }
        public int TruthScore { get; set; }
        public bool IsPinned { get; set; }
        public bool IsRedPill { get; set; }
        public string Tags { get; set; } = string.Empty;
        public string? MediaUrls { get; set; }
        public int ReplyCount { get; set; }
        public int ReactionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsEdited { get; set; }
        public List<RabbitHolePostDto> Replies { get; set; } = new List<RabbitHolePostDto>();
    }

    public class GetRabbitHolePostsQueryHandler : IRequestHandler<GetRabbitHolePostsQuery, GetRabbitHolePostsResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetRabbitHolePostsQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<GetRabbitHolePostsResponse> Handle(GetRabbitHolePostsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            // Get rabbit hole
            var rabbitHole = await _context.RabbitHoles
                .FirstOrDefaultAsync(rh => rh.Id == request.RabbitHoleId, cancellationToken);

            if (rabbitHole == null)
            {
                throw new InvalidOperationException("Rabbit hole not found in the Matrix");
            }

            // Check if user has access to private rabbit hole
            if (!rabbitHole.IsPublic && userId.HasValue)
            {
                var isFollower = await _context.RabbitHoleFollowers
                    .AnyAsync(f => f.RabbitHoleId == request.RabbitHoleId && f.UserId == userId.Value, cancellationToken);

                if (!isFollower)
                {
                    throw new UnauthorizedAccessException("Access denied. This rabbit hole requires following.");
                }
            }

            // Get posts (only top-level, replies are loaded separately)
            var query = _context.RabbitHolePosts
                .Include(p => p.Author)
                .Include(p => p.Replies)
                    .ThenInclude(r => r.Author)
                .Include(p => p.Reactions)
                .Where(p => p.RabbitHoleId == request.RabbitHoleId && p.ParentPostId == null);

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "depth" => query.OrderByDescending(p => p.DepthScore).ThenByDescending(p => p.CreatedAt),
                "truth" => query.OrderByDescending(p => p.TruthScore).ThenByDescending(p => p.CreatedAt),
                "pinned" => query.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = posts.Select(p => MapToDto(p, includeReplies: true)).ToList();

            return new GetRabbitHolePostsResponse
            {
                Posts = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                RabbitHoleName = rabbitHole.Name
            };
        }

        private RabbitHolePostDto MapToDto(RabbitHolePost post, bool includeReplies = false)
        {
            var dto = new RabbitHolePostDto
            {
                Id = post.Id,
                Content = post.Content,
                AuthorId = post.AuthorId,
                AuthorUsername = post.Author?.Username ?? "Unknown",
                AuthorAvatarUrl = post.Author?.AvatarUrl ?? string.Empty,
                ParentPostId = post.ParentPostId,
                DepthScore = post.DepthScore,
                TruthScore = post.TruthScore,
                IsPinned = post.IsPinned,
                IsRedPill = post.IsRedPill,
                Tags = post.Tags,
                MediaUrls = post.MediaUrls,
                ReplyCount = post.Replies?.Count ?? 0,
                ReactionCount = post.Reactions?.Count ?? 0,
                CreatedAt = post.CreatedAt,
                IsEdited = post.IsEdited
            };

            if (includeReplies && post.Replies != null && post.Replies.Any())
            {
                dto.Replies = post.Replies
                    .OrderByDescending(r => r.DepthScore)
                    .ThenByDescending(r => r.CreatedAt)
                    .Take(3) // Only show top 3 replies initially
                    .Select(r => MapToDto(r, includeReplies: false))
                    .ToList();
            }

            return dto;
        }
    }
}
