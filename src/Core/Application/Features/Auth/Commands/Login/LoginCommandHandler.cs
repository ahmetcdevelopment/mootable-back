using MediatR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Features.Auth.Constants;
using Mootable.Application.Features.Auth.Rules;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;

namespace Mootable.Application.Features.Auth.Commands.Login;

/// <summary>
/// Login işleminin handler'ı.
///
/// SECURITY CONSIDERATIONS:
/// 1. Timing attack koruması: Email var/yok fark etmeksizin aynı hata mesajı
/// 2. Brute force koruması: Business rules'da rate limiting kontrolü
/// 3. Password hash comparison: Constant-time comparison (BCrypt internally)
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly AuthBusinessRules _businessRules;

    public LoginCommandHandler(
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

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Using repository pattern with includes
        var user = await _unitOfWork.Users
            .GetQueryableWithIncludes(
                u => u.UserRoles,
                u => u.RefreshTokens)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken);

        _businessRules.UserMustExistForLogin(user);
        _businessRules.PasswordMustBeCorrect(request.Password, user!.PasswordHash);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(request.IpAddress);

        user.RefreshTokens.Add(refreshToken);
        user.Status = UserStatus.Online;
        user.LastSeenAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            DisplayName: user.DisplayName,
            AvatarUrl: user.AvatarUrl,
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15),
            Roles: roles
        );
    }
}
