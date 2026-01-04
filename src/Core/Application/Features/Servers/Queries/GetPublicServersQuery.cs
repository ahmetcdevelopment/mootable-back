using MediatR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Common.Responses;
using Mootable.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Servers.Queries;

/// <summary>
/// Query to get public servers for discovery page
/// Matrix theme: "There is a world within the world - waiting for you"
/// Servers are sorted by popularity and access score
/// </summary>
public class GetPublicServersQuery : IRequest<ServiceResponse<GetPublicServersResponse>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Category { get; set; }
    public string? SearchTerm { get; set; }
    public DiscoverySortBy SortBy { get; set; } = DiscoverySortBy.Popular;
}

public enum DiscoverySortBy
{
    Popular,     // By member count + activity
    Trending,    // High recent activity
    New,         // Recently created
    Alphabetical // A-Z
}

public class GetPublicServersResponse
{
    public List<DiscoveryServerDto> Servers { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }

    // Featured servers (top 5 by score)
    public List<DiscoveryServerDto> Featured { get; set; } = new();
}

public class DiscoveryServerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public int MemberCount { get; set; }
    public string? Category { get; set; }

    // Matrix theme
    public string ShipClass { get; set; } = "Hovercraft";
    public int PowerLevel { get; set; } = 1;

    // Scores
    public int PopularityScore { get; set; }
    public int AccessScore { get; set; }
    public int TotalScore { get; set; }

    // Status
    public int ActiveExplorers { get; set; } // Currently online
    public DateTime? LastActivityAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // For current user
    public bool IsMember { get; set; }
}

public class GetPublicServersQueryHandler : IRequestHandler<GetPublicServersQuery, ServiceResponse<GetPublicServersResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetPublicServersQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResponse<GetPublicServersResponse>> Handle(
        GetPublicServersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;

            // Base query - only public, non-deleted servers
            var query = _unitOfWork.Servers.GetQueryableWithIncludes(
                    s => s.Owner,
                    s => s.Members,
                    s => s.MootTables)
                .Where(s => !s.IsDeleted && s.IsPublic);

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(searchTerm) ||
                    (s.Description != null && s.Description.ToLower().Contains(searchTerm)));
            }

            // Get all servers for scoring
            var servers = await query.ToListAsync(cancellationToken);

            // Calculate scores for each server
            var scoredServers = servers.Select(server =>
            {
                // Calculate popularity score based on:
                // - Member count (40%)
                // - Channel count (20%)
                // - Age bonus for established servers (20%)
                // - Recent activity bonus (20%)

                var memberScore = Math.Min(server.Members.Count * 2, 100); // Max 100 for 50+ members
                var channelScore = Math.Min(server.MootTables.Count * 5, 50); // Max 50 for 10+ channels

                var ageInDays = (DateTime.UtcNow - server.CreatedAt).TotalDays;
                var ageScore = ageInDays switch
                {
                    > 365 => 30, // Veteran servers
                    > 90 => 25,
                    > 30 => 20,
                    > 7 => 10,
                    _ => 0
                };

                // Activity score based on recent joins (simulated)
                var recentActivity = server.Members.Count(m =>
                    m.JoinedAt > DateTime.UtcNow.AddDays(-7));
                var activityScore = Math.Min(recentActivity * 10, 50);

                var popularityScore = (int)((memberScore * 0.4) + (channelScore * 0.2) +
                                            (ageScore * 0.2) + (activityScore * 0.2));

                // Access score based on how easy to find/join
                var accessScore = server.IsPublic ? 50 : 0;
                accessScore += string.IsNullOrEmpty(server.Description) ? 0 : 25;
                accessScore += string.IsNullOrEmpty(server.IconUrl) ? 0 : 25;

                var totalScore = popularityScore + accessScore;

                return new DiscoveryServerDto
                {
                    Id = server.Id,
                    Name = server.Name,
                    Description = server.Description,
                    IconUrl = server.IconUrl,
                    MemberCount = server.Members.Count,
                    ShipClass = GetShipClass(server.Members.Count),
                    PowerLevel = CalculatePowerLevel(server.Members.Count, server.CreatedAt),
                    PopularityScore = popularityScore,
                    AccessScore = accessScore,
                    TotalScore = totalScore,
                    ActiveExplorers = 0, // Would come from presence service
                    LastActivityAt = server.UpdatedAt,
                    CreatedAt = server.CreatedAt,
                    IsMember = currentUserId.HasValue &&
                               server.Members.Any(m => m.UserId == currentUserId.Value)
                };
            }).ToList();

            // Apply sorting
            scoredServers = request.SortBy switch
            {
                DiscoverySortBy.Popular => scoredServers
                    .OrderByDescending(s => s.TotalScore)
                    .ThenByDescending(s => s.MemberCount)
                    .ToList(),
                DiscoverySortBy.Trending => scoredServers
                    .OrderByDescending(s => s.PopularityScore)
                    .ThenByDescending(s => s.CreatedAt)
                    .ToList(),
                DiscoverySortBy.New => scoredServers
                    .OrderByDescending(s => s.CreatedAt)
                    .ToList(),
                DiscoverySortBy.Alphabetical => scoredServers
                    .OrderBy(s => s.Name)
                    .ToList(),
                _ => scoredServers.OrderByDescending(s => s.TotalScore).ToList()
            };

            // Get featured (top 5)
            var featured = scoredServers.Take(5).ToList();

            // Pagination
            var totalCount = scoredServers.Count;
            var skip = (request.PageNumber - 1) * request.PageSize;
            var pagedServers = scoredServers
                .Skip(skip)
                .Take(request.PageSize)
                .ToList();

            var response = new GetPublicServersResponse
            {
                Servers = pagedServers,
                Featured = request.PageNumber == 1 ? featured : new List<DiscoveryServerDto>(),
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                HasNextPage = (request.PageNumber * request.PageSize) < totalCount,
                HasPreviousPage = request.PageNumber > 1
            };

            return ServiceResponse<GetPublicServersResponse>.Success(
                response,
                "Ships detected in the Matrix. Choose your vessel wisely.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<GetPublicServersResponse>.Failure(
                $"Failed to scan the Matrix: {ex.Message}");
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
