using MediatR;

namespace Mootable.Application.Features.Auth.Commands.DeleteAccount;

/// <summary>
/// Command to delete a user account
/// </summary>
public sealed class DeleteAccountCommand : IRequest<DeleteAccountResponse>
{
    /// <summary>
    /// User ID of the account to delete
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Current password for verification
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation text (user must type "DELETE MY ACCOUNT")
    /// </summary>
    public string ConfirmationText { get; set; } = string.Empty;
}

/// <summary>
/// Response for account deletion
/// </summary>
public sealed class DeleteAccountResponse
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