namespace Mootable.Application.Features.Auth.Commands.ExternalLogin;

/// <summary>
/// External login response with tokens and user info
/// </summary>
public sealed record ExternalLoginResponse(
    Guid UserId,
    string Username,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    IReadOnlyList<string> Roles,
    bool IsNewUser,
    string Provider
);