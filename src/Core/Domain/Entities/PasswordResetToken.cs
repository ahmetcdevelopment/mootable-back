using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

/// <summary>
/// Represents a password reset token for user authentication
/// </summary>
public class PasswordResetToken : BaseEntity
{
    /// <summary>
    /// The unique token value
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Email address associated with the reset request
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User ID for the password reset
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Token expiration date/time
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether the token has been used
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Date/time when the token was used (if applicable)
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// IP address from which the reset was requested
    /// </summary>
    public string? RequestedFromIP { get; set; }

    /// <summary>
    /// User agent from which the reset was requested
    /// </summary>
    public string? RequestedUserAgent { get; set; }

    // Navigation properties
    public virtual User? User { get; set; }

    /// <summary>
    /// Checks if the token is still valid
    /// </summary>
    public bool IsValid() => !IsUsed && ExpiresAt > DateTime.UtcNow;

    /// <summary>
    /// Marks the token as used
    /// </summary>
    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }
}