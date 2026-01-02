using Microsoft.EntityFrameworkCore;
using System.Linq;
using Mootable.Domain.Enums;
using MediatR;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mootable.Application.Features.RabbitHoles.Commands
{
    /// <summary>
    /// Follow or unfollow a Rabbit Hole
    /// Matrix theme: "Follow the white rabbit" or "Exit the rabbit hole"
    /// </summary>
    public class FollowRabbitHoleCommand : IRequest<FollowRabbitHoleResponse>
    {
        public Guid RabbitHoleId { get; set; }
        public bool Follow { get; set; } // true to follow, false to unfollow
        public bool NotifyOnNewPosts { get; set; } = true;
        public bool NotifyOnTrending { get; set; } = false;
    }

    public class FollowRabbitHoleResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalFollowers { get; set; }
    }

    public class FollowRabbitHoleCommandHandler : IRequestHandler<FollowRabbitHoleCommand, FollowRabbitHoleResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public FollowRabbitHoleCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _context = context;
            _currentUserService = currentUserService;
            
        }

        public async Task<FollowRabbitHoleResponse> Handle(FollowRabbitHoleCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

            
            

            var rabbitHole = await _context.RabbitHoles.FindAsync(request.RabbitHoleId);
            if (rabbitHole == null)
            {
                throw new InvalidOperationException("Rabbit hole not found in the Matrix");
            }

            var existingFollower = await _context.RabbitHoleFollowers.FirstOrDefaultAsync(
                f => f.RabbitHoleId == request.RabbitHoleId && f.UserId == userId,
                cancellationToken);

            if (request.Follow)
            {
                if (existingFollower != null)
                {
                    // Update notification preferences
                    existingFollower.NotifyOnNewPosts = request.NotifyOnNewPosts;
                    existingFollower.NotifyOnTrending = request.NotifyOnTrending;
                    existingFollower.LastVisitedAt = DateTime.UtcNow;
                    
                }
                else
                {
                    // Create new follower
                    var follower = new RabbitHoleFollower
                    {
                        Id = Guid.NewGuid(),
                        RabbitHoleId = request.RabbitHoleId,
                        UserId = userId,
                        FollowedAt = DateTime.UtcNow,
                        NotifyOnNewPosts = request.NotifyOnNewPosts,
                        NotifyOnTrending = request.NotifyOnTrending,
                        LastVisitedAt = DateTime.UtcNow
                    };
                    
_context.RabbitHoleFollowers.Add(follower);
                }
            }
            else
            {
                if (existingFollower != null)
                {
                    _context.RabbitHoleFollowers.Remove(existingFollower);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Get updated follower count
            var followerCount = await _context.RabbitHoleFollowers.CountAsync(f => f.RabbitHoleId == request.RabbitHoleId, cancellationToken);

            return new FollowRabbitHoleResponse
            {
                Success = true,
                Message = request.Follow 
                    ? $"You are now following the white rabbit into \"{rabbitHole.Name}\""
                    : $"You have exited the rabbit hole \"{rabbitHole.Name}\"",
                TotalFollowers = followerCount
            };
        }
    }
}
