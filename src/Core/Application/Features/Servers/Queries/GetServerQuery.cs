using Mootable.Application.Common.Responses;
using Mootable.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Servers.Queries;

/// <summary>
/// Query to get a single server by ID with full details
/// Matrix theme: "Locate the ship in the Matrix"
/// </summary>
public class GetServerQuery : IRequest<ServiceResponse<ServerDetailDto>>
{
    public Guid ServerId { get; set; }

    public GetServerQuery(Guid serverId)
    {
        ServerId = serverId;
    }
}

/// <summary>
/// Detailed server information including channels and roles
/// </summary>
public class ServerDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int MemberCount { get; set; }
    public bool IsOwner { get; set; }
    public bool IsMember { get; set; }
    public DateTime CreatedAt { get; set; }

    // Owner info
    public Guid OwnerId { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
    public string? OwnerAvatarUrl { get; set; }

    // Channels (MootTables)
    public List<MootTableDto> Channels { get; set; } = new();

    // Roles
    public List<ServerRoleDto> Roles { get; set; } = new();

    // Matrix theme properties
    public string ShipClass { get; set; } = "Hovercraft";
    public int PowerLevel { get; set; } = 1;
}

public class MootTableDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public int Position { get; set; }
    public string Type { get; set; } = "Text";
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsArchived { get; set; }
}

public class ServerRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#00FF00";
    public int Position { get; set; }
    public int MemberCount { get; set; }
}

public class GetServerQueryHandler : IRequestHandler<GetServerQuery, ServiceResponse<ServerDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetServerQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResponse<ServerDetailDto>> Handle(
        GetServerQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;

            // Get server with all necessary includes
            var server = await _unitOfWork.Servers.GetQueryableWithIncludes(
                    s => s.Owner,
                    s => s.Members,
                    s => s.MootTables,
                    s => s.ServerRoles)
                .AsNoTracking()
                .Where(s => s.Id == request.ServerId && !s.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (server == null)
            {
                return ServiceResponse<ServerDetailDto>.Failure(
                    "Ship not found in the Matrix. It may have been unplugged.");
            }

            // Check if user is a member (required to view private servers)
            var isMember = currentUserId.HasValue &&
                           server.Members.Any(m => m.UserId == currentUserId.Value);
            var isOwner = currentUserId.HasValue && server.OwnerId == currentUserId.Value;

            // Private servers require membership
            if (!server.IsPublic && !isMember)
            {
                return ServiceResponse<ServerDetailDto>.Failure(
                    "Access denied. You need an invite to board this ship.");
            }

            // Get categories for channels
            var categoryIds = server.MootTables
                .Where(m => m.CategoryId.HasValue)
                .Select(m => m.CategoryId!.Value)
                .Distinct()
                .ToList();

            var categories = await _unitOfWork.MootTableCategories
                .GetQueryable()
                .Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

            // Map channels
            var channels = server.MootTables
                .OrderBy(m => m.CategoryId)
                .ThenBy(m => m.Position)
                .Select(m => new MootTableDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Topic = m.Topic,
                    Position = m.Position,
                    Type = m.Type.ToString(),
                    CategoryId = m.CategoryId,
                    CategoryName = m.CategoryId.HasValue && categories.ContainsKey(m.CategoryId.Value)
                        ? categories[m.CategoryId.Value]
                        : null,
                    IsArchived = m.IsArchived
                })
                .ToList();

            // Get member counts for roles
            var roleMemberCounts = await _unitOfWork.ServerMemberRoles
                .GetQueryable()
                .Where(smr => server.ServerRoles.Select(sr => sr.Id).Contains(smr.ServerRoleId))
                .GroupBy(smr => smr.ServerRoleId)
                .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);

            // Map roles
            var roles = server.ServerRoles
                .OrderByDescending(r => r.Position)
                .Select(r => new ServerRoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Color = r.Color,
                    Position = r.Position,
                    MemberCount = roleMemberCounts.TryGetValue(r.Id, out var count) ? count : 0
                })
                .ToList();

            var response = new ServerDetailDto
            {
                Id = server.Id,
                Name = server.Name,
                Description = server.Description,
                IconUrl = server.IconUrl,
                InviteCode = isOwner || isMember ? server.InviteCode : string.Empty, // Only show invite code to members
                IsPublic = server.IsPublic,
                MemberCount = server.Members.Count,
                IsOwner = isOwner,
                IsMember = isMember,
                CreatedAt = server.CreatedAt,
                OwnerId = server.OwnerId,
                OwnerUsername = server.Owner.Username,
                OwnerAvatarUrl = server.Owner.AvatarUrl,
                Channels = channels,
                Roles = roles,
                ShipClass = GetShipClass(server.Members.Count),
                PowerLevel = CalculatePowerLevel(server.Members.Count, server.CreatedAt)
            };

            return ServiceResponse<ServerDetailDto>.Success(response,
                "Ship located. Welcome aboard.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<ServerDetailDto>.Failure(
                $"Failed to locate ship: {ex.Message}");
        }
    }

    private static string GetShipClass(int memberCount)
    {
        return memberCount switch
        {
            < 10 => "Hovercraft",
            < 50 => "Transport",
            < 100 => "Warship",
            < 500 => "Battlecruiser",
            _ => "Flagship"
        };
    }

    private static int CalculatePowerLevel(int memberCount, DateTime createdAt)
    {
        var ageInDays = (DateTime.UtcNow - createdAt).Days;
        var ageFactor = Math.Min(ageInDays / 30, 10);
        var memberFactor = Math.Min(memberCount / 10, 10);
        return 1 + ageFactor + memberFactor;
    }
}
