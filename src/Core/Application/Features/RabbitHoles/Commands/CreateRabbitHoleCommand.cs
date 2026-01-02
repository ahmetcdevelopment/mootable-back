using Microsoft.EntityFrameworkCore;
using System.Linq;
using Mootable.Domain.Enums;
using MediatR;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Mootable.Application.Features.RabbitHoles.Commands
{
    /// <summary>
    /// Create a new Rabbit Hole (topic channel)
    /// Matrix theme: "Create a new path down the rabbit hole"
    /// </summary>
    public class CreateRabbitHoleCommand : IRequest<CreateRabbitHoleResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public int DepthLevel { get; set; } = 1;
        public string? ColorHex { get; set; }
        public string? Icon { get; set; }
        public Guid? ParentId { get; set; }
        public string? Tags { get; set; }
        public bool IsPublic { get; set; } = true;
        public int MinimumEnlightenmentScore { get; set; } = 0;
    }

    public class CreateRabbitHoleResponse
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class CreateRabbitHoleCommandHandler : IRequestHandler<CreateRabbitHoleCommand, CreateRabbitHoleResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateRabbitHoleCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _context = context;
            _currentUserService = currentUserService;
            
        }

        public async Task<CreateRabbitHoleResponse> Handle(CreateRabbitHoleCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

            // Generate URL-friendly slug
            var slug = GenerateSlug(request.Name);
            var baseSlug = slug;
            var counter = 1;

            // Ensure unique slug
            
            while (await _context.RabbitHoles.AnyAsync(rh => rh.Slug == slug, cancellationToken))
            {
                slug = $"{baseSlug}-{counter++}";
            }

            var rabbitHole = new RabbitHole
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Topic = request.Topic,
                Slug = slug,
                DepthLevel = request.DepthLevel,
                ColorHex = request.ColorHex ?? "#00FF41", // Matrix green default
                Icon = request.Icon ?? "🐇",
                ParentId = request.ParentId,
                Tags = request.Tags ?? string.Empty,
                IsPublic = request.IsPublic,
                MinimumEnlightenmentScore = request.MinimumEnlightenmentScore,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            
_context.RabbitHoles.Add(rabbitHole);
            await _context.SaveChangesAsync(cancellationToken);

            return new CreateRabbitHoleResponse
            {
                Id = rabbitHole.Id,
                Slug = rabbitHole.Slug,
                Name = rabbitHole.Name,
                Message = $"New rabbit hole \"{rabbitHole.Name}\" created. Follow the white rabbit to /rabbit-holes/{rabbitHole.Slug}"
            };
        }

        private string GenerateSlug(string name)
        {
            // Convert to lowercase
            var slug = name.ToLowerInvariant();

            // Replace spaces with hyphens
            slug = Regex.Replace(slug, @"\s+", "-");

            // Remove invalid characters
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

            // Remove duplicate hyphens
            slug = Regex.Replace(slug, @"-+", "-");

            // Trim hyphens from start and end
            slug = slug.Trim('-');

            return string.IsNullOrEmpty(slug) ? "rabbit-hole" : slug;
        }
    }
}
