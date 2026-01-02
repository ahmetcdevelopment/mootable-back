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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly AuthBusinessRules _businessRules;

    public RegisterCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        AuthBusinessRules businessRules)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _businessRules = businessRules;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if email exists using repository pattern
        var emailExists = await _unitOfWork.Users
            .ExistsAsync(u => u.Email == request.Email, cancellationToken);
        _businessRules.EmailMustBeUnique(emailExists);

        // Check if username exists using repository pattern
        var usernameExists = await _unitOfWork.Users
            .ExistsAsync(u => u.Username == request.Username, cancellationToken);
        _businessRules.UsernameMustBeUnique(usernameExists);

        // Get user role
        var userRole = await _unitOfWork.Roles
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

        // Use repository to add user
        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
