using MediatR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Features.Auth.Rules;
using Mootable.Application.Interfaces;

namespace Mootable.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Refresh token rotation implementasyonu.
/// 
/// SECURITY BEST PRACTICE:
/// Her refresh token kullanımında yeni bir refresh token üretilir.
/// Eski token revoke edilir.
/// Bu sayede stolen token detection mümkün olur:
/// - Token çalınır ve kullanılır = yeni token üretilir
/// - Gerçek kullanıcı eski token'ı kullanmaya çalışır = revoked hatası
/// - Tüm tokenlar revoke edilir, kullanıcı yeniden login olur
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly AuthBusinessRules _businessRules;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        AuthBusinessRules businessRules)
    {
        _context = context;
        _tokenService = tokenService;
        _businessRules = businessRules;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.Token, cancellationToken);

        _businessRules.RefreshTokenMustBeValid(existingToken);
        _businessRules.UserMustNotBeDeleted(existingToken!.User);

        var user = existingToken.User;
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.RevokedByIp = request.IpAddress;

        var newRefreshToken = _tokenService.GenerateRefreshToken(request.IpAddress);
        existingToken.ReplacedByToken = newRefreshToken.Token;
        
        newRefreshToken.UserId = user.Id;
        _context.RefreshTokens.Add(newRefreshToken);

        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);

        await _context.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken.Token,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15)
        );
    }
}
