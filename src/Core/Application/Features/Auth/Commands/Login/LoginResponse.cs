namespace Mootable.Application.Features.Auth.Commands.Login;

public sealed record LoginResponse(
    Guid UserId,
    string Username,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    IReadOnlyList<string> Roles
);
