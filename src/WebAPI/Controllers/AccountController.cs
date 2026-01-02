using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mootable.Application.Features.Auth.Commands.DeleteAccount;
using System.Security.Claims;

namespace Mootable.WebAPI.Controllers;

/// <summary>
/// Account management controller
/// </summary>
[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IMediator mediator, ILogger<AccountController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Delete the current user's account
    /// </summary>
    /// <param name="request">Account deletion request</param>
    /// <returns>Response with deletion status</returns>
    [HttpDelete("delete")]
    [ProducesResponseType(typeof(DeleteAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            var command = new DeleteAccountCommand
            {
                UserId = userId,
                Password = request.Password,
                ConfirmationText = request.ConfirmationText
            };

            var response = await _mediator.Send(command);

            if (response.Success)
            {
                _logger.LogInformation("Account deleted for user {UserId}", userId);
                return Ok(response);
            }

            return BadRequest(new { message = response.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteAccount endpoint");
            return StatusCode(500, new { message = "An error occurred processing your request" });
        }
    }

    /// <summary>
    /// Get account deletion requirements
    /// </summary>
    /// <returns>Requirements for account deletion</returns>
    [HttpGet("delete-requirements")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetDeleteRequirements()
    {
        return Ok(new
        {
            requirements = new[]
            {
                "Enter your current password",
                "Type 'DELETE MY ACCOUNT' in the confirmation field",
                "Click the delete button"
            },
            warnings = new[]
            {
                "This action is permanent and cannot be undone",
                "All your personal data will be removed",
                "Your messages will be anonymized but preserved",
                "You will lose access to all servers and channels"
            },
            confirmationText = "DELETE MY ACCOUNT"
        });
    }
}

/// <summary>
/// Request model for account deletion
/// </summary>
public class DeleteAccountRequest
{
    /// <summary>
    /// Current password for verification
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation text (must be "DELETE MY ACCOUNT")
    /// </summary>
    public string ConfirmationText { get; set; } = string.Empty;
}