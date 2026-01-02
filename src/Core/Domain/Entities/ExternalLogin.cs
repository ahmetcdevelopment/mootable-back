using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

/// <summary>
/// External authentication provider login information
/// </summary>
public sealed class ExternalLogin : BaseEntity
{
    public required Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// Provider name (Google, Microsoft, etc.)
    /// </summary>
    public required string Provider { get; set; }

    /// <summary>
    /// Provider's user identifier
    /// </summary>
    public required string ProviderKey { get; set; }

    /// <summary>
    /// Display name from provider
    /// </summary>
    public string? ProviderDisplayName { get; set; }

    /// <summary>
    /// Email from provider
    /// </summary>
    public string? ProviderEmail { get; set; }

    /// <summary>
    /// Profile picture URL from provider
    /// </summary>
    public string? ProviderPhotoUrl { get; set; }

    /// <summary>
    /// Access token from provider (optional, for API calls)
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Refresh token from provider (optional)
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token expiry time
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }
}

/// <summary>
/// Supported external authentication providers
/// </summary>
public static class ExternalLoginProviders
{
    public const string Google = "Google";
    public const string Microsoft = "Microsoft";
    public const string GitHub = "GitHub";
    public const string Discord = "Discord";
}