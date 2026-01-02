using MediatR;

namespace Mootable.Application.Features.Auth.Commands.ExternalLogin;

/// <summary>
/// External login command for OAuth2 providers
/// </summary>
public sealed record ExternalLoginCommand : IRequest<ExternalLoginResponse>
{
    /// <summary>
    /// Provider name (Google, Microsoft, etc.)
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Provider's user identifier
    /// </summary>
    public required string ProviderKey { get; init; }

    /// <summary>
    /// Email from provider
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Display name from provider
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Profile picture URL from provider
    /// </summary>
    public string? PhotoUrl { get; init; }

    /// <summary>
    /// Access token from provider (optional)
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Refresh token from provider (optional)
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Token expiry time
    /// </summary>
    public DateTime? TokenExpiresAt { get; init; }

    /// <summary>
    /// Client IP address for refresh token
    /// </summary>
    public required string IpAddress { get; init; }
}