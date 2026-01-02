using Mootable.Application.Common.Responses;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Servers.Queries
{
    public class GetServersQuery : IRequest<ServiceResponse<GetServersResponseDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool OnlyMyServers { get; set; } = false;
        public bool OnlyPublic { get; set; } = false;
        public string? SearchTerm { get; set; }
    }

    public class GetServersResponseDto
    {
        public List<ServerDto> Servers { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class ServerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public bool IsPublic { get; set; }
        public int MemberCount { get; set; }
        public bool IsOwner { get; set; }
        public bool IsMember { get; set; }
        public string? OwnerUsername { get; set; }
        public DateTime CreatedAt { get; set; }

        // Matrix theme properties
        public string ShipClass { get; set; } = "Hovercraft";
        public int PowerLevel { get; set; } = 1;
    }

    public class GetServersQueryHandler : IRequestHandler<GetServersQuery, ServiceResponse<GetServersResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetServersQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<ServiceResponse<GetServersResponseDto>> Handle(
            GetServersQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;

                var query = _unitOfWork.Servers.GetQueryableWithIncludes(
                        s => s.Owner,
                        s => s.Members)
                    .Where(s => !s.IsDeleted);

                // Filter by user's servers
                if (request.OnlyMyServers && currentUserId.HasValue)
                {
                    query = query.Where(s => s.Members.Any(m => m.UserId == currentUserId.Value));
                }

                // Filter by public servers
                if (request.OnlyPublic)
                {
                    query = query.Where(s => s.IsPublic);
                }

                // Search filter
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    query = query.Where(s =>
                        s.Name.ToLower().Contains(searchTerm) ||
                        (s.Description != null && s.Description.ToLower().Contains(searchTerm)));
                }

                // Get total count
                var totalCount = await query.CountAsync(cancellationToken);

                // Apply pagination
                var servers = await query
                    .OrderByDescending(s => s.Members.Count)
                    .ThenByDescending(s => s.CreatedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                // Map to DTOs
                var serverDtos = servers.Select(s => new ServerDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    IconUrl = s.IconUrl,
                    IsPublic = s.IsPublic,
                    MemberCount = s.Members.Count,
                    IsOwner = currentUserId.HasValue && s.OwnerId == currentUserId.Value,
                    IsMember = currentUserId.HasValue && s.Members.Any(m => m.UserId == currentUserId.Value),
                    OwnerUsername = s.Owner.Username,
                    CreatedAt = s.CreatedAt,
                    ShipClass = GetShipClass(s.Members.Count),
                    PowerLevel = CalculatePowerLevel(s.Members.Count, s.CreatedAt)
                }).ToList();

                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

                var response = new GetServersResponseDto
                {
                    Servers = serverDtos,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.PageNumber < totalPages,
                    HasPreviousPage = request.PageNumber > 1
                };

                return ServiceResponse<GetServersResponseDto>.Success(response,
                    "Ships detected in the Matrix.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<GetServersResponseDto>.Failure(
                    $"Failed to scan for ships: {ex.Message}");
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
            var ageFactor = Math.Min(ageInDays / 30, 10); // Max 10 points for age
            var memberFactor = Math.Min(memberCount / 10, 10); // Max 10 points for members

            return 1 + ageFactor + memberFactor; // Base level 1
        }
    }
}