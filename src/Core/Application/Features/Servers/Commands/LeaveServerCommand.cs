using Mootable.Application.Common.Responses;
using Mootable.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Servers.Commands;

/// <summary>
/// Command to leave a server (abandon ship)
/// Matrix theme: "Abandon ship, return to Zion"
/// </summary>
public class LeaveServerCommand : IRequest<ServiceResponse<LeaveServerResponseDto>>
{
    public Guid ServerId { get; set; }

    public LeaveServerCommand(Guid serverId)
    {
        ServerId = serverId;
    }
}

public class LeaveServerResponseDto
{
    public string ServerName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class LeaveServerCommandHandler : IRequestHandler<LeaveServerCommand, ServiceResponse<LeaveServerResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public LeaveServerCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResponse<LeaveServerResponseDto>> Handle(
        LeaveServerCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedAccessException("User not authenticated");

            // Get the server
            var server = await _unitOfWork.Servers
                .FirstOrDefaultAsync(s => s.Id == request.ServerId && !s.IsDeleted, cancellationToken);

            if (server == null)
            {
                return ServiceResponse<LeaveServerResponseDto>.Failure(
                    "Ship not found in the Matrix.");
            }

            // Prevent owner from leaving (must transfer or delete)
            if (server.OwnerId == currentUserId)
            {
                return ServiceResponse<LeaveServerResponseDto>.Failure(
                    "The Captain cannot abandon ship. Transfer ownership or destroy the ship first.");
            }

            // Find the membership
            var membership = await _unitOfWork.ServerMembers
                .FirstOrDefaultAsync(m => m.ServerId == request.ServerId && m.UserId == currentUserId,
                    cancellationToken);

            if (membership == null)
            {
                return ServiceResponse<LeaveServerResponseDto>.Failure(
                    "You are not a crew member of this ship.");
            }

            // Remove member roles first
            var memberRoles = await _unitOfWork.ServerMemberRoles
                .GetQueryable()
                .Where(mr => mr.ServerMemberId == membership.Id)
                .ToListAsync(cancellationToken);

            foreach (var role in memberRoles)
            {
                _unitOfWork.ServerMemberRoles.Delete(role);
            }

            // Remove membership
            _unitOfWork.ServerMembers.Delete(membership);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ServiceResponse<LeaveServerResponseDto>.Success(
                new LeaveServerResponseDto
                {
                    ServerName = server.Name,
                    Message = $"You have left the {server.Name}. Safe travels in the Matrix."
                });
        }
        catch (Exception ex)
        {
            return ServiceResponse<LeaveServerResponseDto>.Failure(
                $"Failed to leave ship: {ex.Message}");
        }
    }
}
