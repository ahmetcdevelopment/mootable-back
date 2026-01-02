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
    /// Get all public Rabbit Holes
    /// Matrix theme: "Discover all paths down the rabbit hole"
    /// </summary>
    public class GetRabbitHolesQuery : IRequest<GetRabbitHolesResponse>
    {
        public string? Topic { get; set; } // Filter by topic
        public int? MinDepth { get; set; } // Minimum depth level
        public int? MaxDepth { get; set; } // Maximum depth level
        public bool OnlyFollowed { get; set; } = false; // Only show followed rabbit holes
    }

    public class GetRabbitHolesResponse
    {
        public List<RabbitHoleDto> RabbitHoles { get; set; } = new List<RabbitHoleDto>();
        public int TotalCount { get; set; }
    }

    public class RabbitHoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int DepthLevel { get; set; }
        public string ColorHex { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int PostCount { get; set; }
        public int FollowerCount { get; set; }
        public int ActiveExplorers { get; set; }
        public bool IsFollowing { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<RabbitHoleDto> SubHoles { get; set; } = new List<RabbitHoleDto>();
    }

    public class GetRabbitHolesQueryHandler : IRequestHandler<GetRabbitHolesQuery, GetRabbitHolesResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetRabbitHolesQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<GetRabbitHolesResponse> Handle(GetRabbitHolesQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var query = _context.RabbitHoles
                .Include(rh => rh.SubHoles)
                .Include(rh => rh.Followers)
                .Where(rh => rh.IsPublic && rh.ParentId == null); // Only get top-level public rabbit holes

            // Apply filters
            if (!string.IsNullOrEmpty(request.Topic))
            {
                query = query.Where(rh => rh.Topic.ToLower().Contains(request.Topic.ToLower()));
            }

            if (request.MinDepth.HasValue)
            {
                query = query.Where(rh => rh.DepthLevel >= request.MinDepth.Value);
            }

            if (request.MaxDepth.HasValue)
            {
                query = query.Where(rh => rh.DepthLevel <= request.MaxDepth.Value);
            }

            if (request.OnlyFollowed && userId.HasValue)
            {
                query = query.Where(rh => rh.Followers.Any(f => f.UserId == userId.Value));
            }

            var rabbitHoles = await query
                .OrderByDescending(rh => rh.PostCount)
                .ThenByDescending(rh => rh.Followers.Count)
                .ToListAsync(cancellationToken);

            var dtos = rabbitHoles.Select(rh => MapToDto(rh, userId)).ToList();

            return new GetRabbitHolesResponse
            {
                RabbitHoles = dtos,
                TotalCount = dtos.Count
            };
        }

        private RabbitHoleDto MapToDto(RabbitHole rabbitHole, Guid? userId)
        {
            return new RabbitHoleDto
            {
                Id = rabbitHole.Id,
                Name = rabbitHole.Name,
                Description = rabbitHole.Description,
                Topic = rabbitHole.Topic,
                Slug = rabbitHole.Slug,
                DepthLevel = rabbitHole.DepthLevel,
                ColorHex = rabbitHole.ColorHex,
                Icon = rabbitHole.Icon,
                PostCount = rabbitHole.PostCount,
                FollowerCount = rabbitHole.Followers.Count,
                ActiveExplorers = rabbitHole.ActiveExplorers,
                IsFollowing = userId.HasValue && rabbitHole.Followers.Any(f => f.UserId == userId.Value),
                CreatedAt = rabbitHole.CreatedAt,
                SubHoles = rabbitHole.SubHoles.Where(sh => sh.IsPublic).Select(sh => MapToDto(sh, userId)).ToList()
            };
        }
    }
}
