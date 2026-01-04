using MediatR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Features.Auth.Constants;
using Mootable.Application.Features.Auth.Rules;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using Mootable.Domain.Events;

namespace Mootable.Application.Features.Auth.Commands.ExternalLogin;

/// <summary>
/// Handles external login via OAuth2 providers
/// Creates new user if not exists or links to existing user
/// </summary>
public sealed class ExternalLoginCommandHandler : IRequestHandler<ExternalLoginCommand, ExternalLoginResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly AuthBusinessRules _businessRules;

    public ExternalLoginCommandHandler(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        AuthBusinessRules businessRules)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _businessRules = businessRules;
    }

    public async Task<ExternalLoginResponse> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        // Check if external login exists
        var externalLogin = await _unitOfWork.Repository<Domain.Entities.ExternalLogin>()
            .GetQueryableWithIncludes(el => el.User!)
            .Include(el => el.User!.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(el =>
                el.Provider == request.Provider &&
                el.ProviderKey == request.ProviderKey &&
                !el.IsDeleted,
                cancellationToken);

        User? user;
        bool isNewUser = false;

        if (externalLogin != null)
        {
            // Existing external login - use existing user
            user = externalLogin.User!;

            // Update external login info
            externalLogin.ProviderEmail = request.Email;
            externalLogin.ProviderDisplayName = request.DisplayName;
            externalLogin.ProviderPhotoUrl = request.PhotoUrl;
            externalLogin.AccessToken = request.AccessToken;
            externalLogin.RefreshToken = request.RefreshToken;
            externalLogin.TokenExpiresAt = request.TokenExpiresAt;
            externalLogin.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Domain.Entities.ExternalLogin>().Update(externalLogin);
            
            // Update user status
            user.Status = UserStatus.Online;
            user.LastSeenAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
        }
        else
        {
            // Check if user exists with this email
            user = await _unitOfWork.Users
                .GetQueryableWithIncludes(u => u.UserRoles)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                // Create new user
                isNewUser = true;
                var userRole = await _unitOfWork.Roles
                    .FirstOrDefaultAsync(r => r.Name == AuthRoles.User, cancellationToken);

                // Generate unique username from email
                var baseUsername = request.Email.Split('@')[0];
                var username = await GenerateUniqueUsername(baseUsername, cancellationToken);

                user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    Email = request.Email,
                    PasswordHash = GenerateRandomPassword(),
                    DisplayName = request.DisplayName ?? username,
                    AvatarUrl = request.PhotoUrl,
                    Status = UserStatus.Online,
                    CreatedAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow
                };

                user.CreatedBy = user.Id;

                await _unitOfWork.Users.AddAsync(user, cancellationToken);
                
                // IMPORTANT: Save user first before adding related entities
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Now add UserRole after user is saved
                if (userRole != null)
                {
                    var newUserRole = new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        RoleId = userRole.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<UserRole>().AddAsync(newUserRole, cancellationToken);
                }

                // Add domain event for new user
                user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Username, user.Email));
            }
            else
            {
                // Existing user - update status
                user.Status = UserStatus.Online;
                user.LastSeenAt = DateTime.UtcNow;
                if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(request.PhotoUrl))
                {
                    user.AvatarUrl = request.PhotoUrl;
                }
                _unitOfWork.Users.Update(user);
            }

            // Create external login entry (user now exists in DB)
            var newExternalLogin = new Domain.Entities.ExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = request.Provider,
                ProviderKey = request.ProviderKey,
                ProviderEmail = request.Email,
                ProviderDisplayName = request.DisplayName,
                ProviderPhotoUrl = request.PhotoUrl,
                AccessToken = request.AccessToken,
                RefreshToken = request.RefreshToken,
                TokenExpiresAt = request.TokenExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Domain.Entities.ExternalLogin>().AddAsync(newExternalLogin, cancellationToken);
        }

        // Generate tokens
        var roles = user.UserRoles?.Select(ur => ur.Role?.Name ?? "User").ToList() ?? new List<string> { "User" };
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(request.IpAddress);

        // Set the UserId on refresh token and add directly to repository
        refreshToken.UserId = user.Id;
        await _unitOfWork.Repository<Domain.Entities.RefreshToken>().AddAsync(refreshToken, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExternalLoginResponse(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            DisplayName: user.DisplayName,
            AvatarUrl: user.AvatarUrl,
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15),
            Roles: roles,
            IsNewUser: isNewUser,
            Provider: request.Provider
        );
    }

    private async Task<string> GenerateUniqueUsername(string baseUsername, CancellationToken cancellationToken)
    {
        // Remove special characters and spaces
        baseUsername = System.Text.RegularExpressions.Regex.Replace(baseUsername, @"[^a-zA-Z0-9]", "");

        if (string.IsNullOrEmpty(baseUsername))
        {
            baseUsername = "user";
        }

        var username = baseUsername;
        var counter = 1;

        while (await _unitOfWork.Users.ExistsAsync(u => u.Username == username, cancellationToken))
        {
            username = $"{baseUsername}{counter}";
            counter++;
        }

        return username;
    }

    private string GenerateRandomPassword()
    {
        // Generate a random password that the user will never know
        // They can only login via OAuth2
        return $"OAuth2_{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}";
    }
}