using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mootable.Application.Features.Auth.Commands.RequestPasswordReset;
using Mootable.Application.Features.Auth.Commands.ResetPassword;

namespace Mootable.WebAPI.Controllers;

/// <summary>
/// Password reset controller
/// </summary>
[ApiController]
[Route("api/auth/password")]
[AllowAnonymous]
public class PasswordResetController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PasswordResetController> _logger;

    public PasswordResetController(IMediator mediator, ILogger<PasswordResetController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Request a password reset email
    /// </summary>
    /// <param name="request">Password reset request</param>
    /// <returns>Response with success status</returns>
    [HttpPost("forgot")]
    [ProducesResponseType(typeof(RequestPasswordResetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] RequestPasswordResetCommand request)
    {
        try
        {
            // Add client IP and user agent
            request.ClientIP = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.UserAgent = Request.Headers["User-Agent"].ToString();

            var response = await _mediator.Send(request);

            if (response.Success)
            {
                return Ok(response);
            }

            return BadRequest(new { message = response.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ForgotPassword endpoint");
            return StatusCode(500, new { message = "An error occurred processing your request" });
        }
    }

    /// <summary>
    /// Reset password using a token
    /// </summary>
    /// <param name="request">Password reset with token</param>
    /// <returns>Response with success status</returns>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand request)
    {
        try
        {
            var response = await _mediator.Send(request);

            if (response.Success)
            {
                return Ok(response);
            }

            return BadRequest(new { message = response.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResetPassword endpoint");
            return StatusCode(500, new { message = "An error occurred processing your request" });
        }
    }

    /// <summary>
    /// Validate a password reset token
    /// </summary>
    /// <param name="token">Reset token to validate</param>
    /// <returns>Token validity status</returns>
    [HttpGet("validate-token")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateToken([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { valid = false, message = "Token is required" });
        }

        // TODO: Implement token validation logic
        // For now, just return a mock response
        return Ok(new { valid = true, message = "Token is valid" });
    }
}