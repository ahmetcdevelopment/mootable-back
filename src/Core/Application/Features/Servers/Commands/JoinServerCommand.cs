using Mootable.Application.Common.Responses;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Servers.Commands
{
    public class JoinServerCommand : IRequest<ServiceResponse<JoinServerResponseDto>>
    {
        public string InviteCode { get; set; } = string.Empty;
    }

    public class JoinServerResponseDto
    {
        public Guid ServerId { get; set; }
        public string ServerName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class JoinServerCommandHandler : IRequestHandler<JoinServerCommand, ServiceResponse<JoinServerResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public JoinServerCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResponse<JoinServerResponseDto>> Handle(
            JoinServerCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = _currentUserService.UserId
                    ?? throw new UnauthorizedAccessException("User not authenticated");

                // Find the ship by invite code
                var server = await _unitOfWork.Servers.GetQueryableWithIncludes(
                        s => s.Members,
                        s => s.ServerRoles)
                    .FirstOrDefaultAsync(s => s.InviteCode == request.InviteCode && !s.IsDeleted,
                        cancellationToken);

                if (server == null)
                {
                    return ServiceResponse<JoinServerResponseDto>.Failure(
                        "Invalid transmission code. No ship found at these coordinates.");
                }

                // Check if user is already a crew member
                var existingMember = await _unitOfWork.ServerMembers
                    .FirstOrDefaultAsync(m => m.ServerId == server.Id && m.UserId == currentUserId,
                        cancellationToken);

                if (existingMember != null)
                {
                    return ServiceResponse<JoinServerResponseDto>.Failure(
                        "You're already part of this ship's crew.");
                }

                // Add user as crew member
                var newMember = new ServerMember
                {
                    Id = Guid.NewGuid(),
                    ServerId = server.Id,
                    UserId = currentUserId,
                    JoinedAt = DateTime.UtcNow
                };

                await _unitOfWork.ServerMembers.AddAsync(newMember, cancellationToken);

                // Assign default "Crew" role (lowest position role)
                var defaultRole = server.ServerRoles.OrderBy(r => r.Position).FirstOrDefault();
                if (defaultRole != null)
                {
                    var memberRole = new ServerMemberRole
                    {
                        Id = Guid.NewGuid(),
                        ServerMemberId = newMember.Id,
                        ServerRoleId = defaultRole.Id
                    };

                    await _unitOfWork.ServerMemberRoles.AddAsync(memberRole, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new JoinServerResponseDto
                {
                    ServerId = server.Id,
                    ServerName = server.Name,
                    MemberCount = server.Members.Count + 1,
                    Message = $"Welcome aboard the {server.Name}. You are now part of the crew."
                };

                return ServiceResponse<JoinServerResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                return ServiceResponse<JoinServerResponseDto>.Failure(
                    $"Failed to board the ship: {ex.Message}");
            }
        }
    }
}