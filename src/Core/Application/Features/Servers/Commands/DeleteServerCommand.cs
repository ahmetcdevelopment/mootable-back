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
/// Command to delete a server (destroy ship)
/// Matrix theme: "Self-destruct sequence initiated"
/// Only the Captain (owner) can destroy their ship.
/// Uses soft delete to preserve data integrity.
/// </summary>
public class DeleteServerCommand : IRequest<ServiceResponse<DeleteServerResponseDto>>
{
    public Guid ServerId { get; set; }

    public DeleteServerCommand(Guid serverId)
    {
        ServerId = serverId;
    }
}

public class DeleteServerResponseDto
{
    public string ServerName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class DeleteServerCommandHandler : IRequestHandler<DeleteServerCommand, ServiceResponse<DeleteServerResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteServerCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResponse<DeleteServerResponseDto>> Handle(
        DeleteServerCommand request,
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
                return ServiceResponse<DeleteServerResponseDto>.Failure(
                    "Ship not found in the Matrix.");
            }

            // Only owner can delete
            if (server.OwnerId != currentUserId)
            {
                return ServiceResponse<DeleteServerResponseDto>.Failure(
                    "Only the Captain can initiate self-destruct. Access denied.");
            }

            // Soft delete the server
            server.IsDeleted = true;
            server.UpdatedAt = DateTime.UtcNow;
            server.UpdatedBy = currentUserId;

            _unitOfWork.Servers.Update(server);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ServiceResponse<DeleteServerResponseDto>.Success(
                new DeleteServerResponseDto
                {
                    ServerName = server.Name,
                    Message = $"The {server.Name} has been destroyed. All crew have been disconnected."
                });
        }
        catch (Exception ex)
        {
            return ServiceResponse<DeleteServerResponseDto>.Failure(
                $"Self-destruct failed: {ex.Message}");
        }
    }
}
