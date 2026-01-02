using MediatR;

namespace Mootable.Application.Features.Auth.Commands.ResetPassword;

/// <summary>
/// Command to reset password using a token
/// </summary>
public sealed class ResetPasswordCommand : IRequest<ResetPasswordResponse>
{
    /// <summary>
    /// Password reset token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirm new password
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Response for password reset
/// </summary>
public sealed class ResetPasswordResponse
{
    /// <summary>
    /// Success status
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message to display to the user
    /// </summary>
    public string Message { get; set; } = string.Empty;
}