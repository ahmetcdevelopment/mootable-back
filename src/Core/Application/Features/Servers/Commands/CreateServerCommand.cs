using Mootable.Application.Common.Responses;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Servers.Commands
{
    public class CreateServerCommand : IRequest<ServiceResponse<ServerResponseDto>>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public bool IsPublic { get; set; } = false;
    }

    public class ServerResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public string InviteCode { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerUsername { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateServerCommandHandler : IRequestHandler<CreateServerCommand, ServiceResponse<ServerResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public CreateServerCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResponse<ServerResponseDto>> Handle(
            CreateServerCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = _currentUserService.UserId
                    ?? throw new UnauthorizedAccessException("User not authenticated");

                // Create the ship (server)
                var server = new Server
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Description = request.Description,
                    IconUrl = request.IconUrl,
                    InviteCode = GenerateInviteCode(),
                    IsPublic = request.IsPublic,
                    OwnerId = currentUserId,
                    CreatedBy = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Servers.AddAsync(server, cancellationToken);

                // Add owner as the first crew member (with Captain role)
                var ownerMember = new ServerMember
                {
                    Id = Guid.NewGuid(),
                    ServerId = server.Id,
                    UserId = currentUserId,
                    JoinedAt = DateTime.UtcNow
                };

                await _unitOfWork.ServerMembers.AddAsync(ownerMember, cancellationToken);

                // Create default roles
                var captainRole = new ServerRole
                {
                    Id = Guid.NewGuid(),
                    ServerId = server.Id,
                    Name = "Captain",
                    Color = "#00FF00", // Matrix green
                    Permissions = ServerPermissions.Administrator,
                    Position = 100
                };

                var operatorRole = new ServerRole
                {
                    Id = Guid.NewGuid(),
                    ServerId = server.Id,
                    Name = "Operator",
                    Color = "#00AA00",
                    Permissions = ServerPermissions.ManageMessages | ServerPermissions.KickMembers,
                    Position = 50
                };

                var crewRole = new ServerRole
                {
                    Id = Guid.NewGuid(),
                    ServerId = server.Id,
                    Name = "Crew",
                    Color = "#008800",
                    Permissions = ServerPermissions.ViewMootTables | ServerPermissions.SendMessages,
                    Position = 10
                };

                await _unitOfWork.ServerRoles.AddAsync(captainRole, cancellationToken);
                await _unitOfWork.ServerRoles.AddAsync(operatorRole, cancellationToken);
                await _unitOfWork.ServerRoles.AddAsync(crewRole, cancellationToken);

                // Assign Captain role to owner
                var ownerRole = new ServerMemberRole
                {
                    Id = Guid.NewGuid(),
                    ServerMemberId = ownerMember.Id,
                    ServerRoleId = captainRole.Id
                };

                await _unitOfWork.ServerMemberRoles.AddAsync(ownerRole, cancellationToken);

                // Create default channels (MootTables)
                var generalChannel = new MootTable
                {
                    Id = Guid.NewGuid(),
                    ServerId = server.Id,
                    Name = "bridge", // Matrix theme: ship's bridge
                    Topic = "Main command center",
                    Type = MootTableType.Text,
                    Position = 0,
                    CreatedBy = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };

                var announcementChannel = new MootTable
                {
                    Id = Guid.NewGuid(),
                    ServerId = server.Id,
                    Name = "transmissions", // Matrix theme: communications
                    Topic = "Important ship announcements",
                    Type = MootTableType.Announcement,
                    Position = 1,
                    CreatedBy = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.MootTables.AddAsync(generalChannel, cancellationToken);
                await _unitOfWork.MootTables.AddAsync(announcementChannel, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Get the created server with owner info
                var createdServer = await _unitOfWork.Servers.GetQueryableWithIncludes(
                        s => s.Owner,
                        s => s.Members)
                    .FirstOrDefaultAsync(s => s.Id == server.Id, cancellationToken);

                var response = new ServerResponseDto
                {
                    Id = createdServer!.Id,
                    Name = createdServer.Name,
                    Description = createdServer.Description,
                    IconUrl = createdServer.IconUrl,
                    InviteCode = createdServer.InviteCode,
                    IsPublic = createdServer.IsPublic,
                    OwnerId = createdServer.OwnerId,
                    OwnerUsername = createdServer.Owner.Username,
                    MemberCount = createdServer.Members.Count,
                    CreatedAt = createdServer.CreatedAt
                };

                return ServiceResponse<ServerResponseDto>.Success(
                    response,
                    "Welcome aboard the ship, Captain. Your crew awaits.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<ServerResponseDto>.Failure(
                    $"Failed to launch ship: {ex.Message}");
            }
        }

        private static string GenerateInviteCode()
        {
            // Generate Matrix-style invite code
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var code = new char[8];

            for (int i = 0; i < code.Length; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
            }

            return $"MX-{new string(code)}"; // MX prefix for Matrix theme
        }
    }

}