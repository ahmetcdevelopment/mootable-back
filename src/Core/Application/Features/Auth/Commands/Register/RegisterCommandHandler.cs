using MediatR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Features.Auth.Constants;
using Mootable.Application.Features.Auth.Rules;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using Mootable.Domain.Events;

namespace Mootable.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly AuthBusinessRules _businessRules;

    public RegisterCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        AuthBusinessRules businessRules)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _businessRules = businessRules;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken);
        _businessRules.EmailMustBeUnique(emailExists);

        var usernameExists = await _context.Users
            .AnyAsync(u => u.Username == request.Username && !u.IsDeleted, cancellationToken);
        _businessRules.UsernameMustBeUnique(usernameExists);

        var userRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == AuthRoles.User, cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            DisplayName = request.DisplayName ?? request.Username,
            Status = UserStatus.Online,
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };

        user.CreatedBy = user.Id;

        if (userRole != null)
        {
            user.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = userRole.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        var refreshToken = _tokenService.GenerateRefreshToken(request.IpAddress);
        user.RefreshTokens.Add(refreshToken);

        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Username, user.Email));

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        var roles = userRole != null ? new[] { userRole.Name } : Array.Empty<string>();
        var accessToken = _tokenService.GenerateAccessToken(user, roles);

        return new RegisterResponse(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15)
        );
    }
}
