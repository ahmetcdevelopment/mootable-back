using MediatR;

namespace Mootable.Application.Features.Auth.Commands.RequestPasswordReset;

/// <summary>
/// Command to request a password reset
/// </summary>
public sealed class RequestPasswordResetCommand : IRequest<RequestPasswordResetResponse>
{
    /// <summary>
    /// Email address for the password reset request
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Client IP address (optional)
    /// </summary>
    public string? ClientIP { get; set; }

    /// <summary>
    /// User agent (optional)
    /// </summary>
    public string? UserAgent { get; set; }
}

/// <summary>
/// Response for password reset request
/// </summary>
public sealed class RequestPasswordResetResponse
{
    /// <summary>
    /// Success status
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message to display to the user
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Reset token (only for development/testing)
    /// </summary>
    public string? ResetToken { get; set; }
}