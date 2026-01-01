using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mootable.Application.Features.Auth.Commands.Login;
using Mootable.Application.Features.Auth.Commands.RefreshToken;
using Mootable.Application.Features.Auth.Commands.Register;

namespace Mootable.WebAPI.Controllers;

/// <summary>
/// Authentication endpoints.
/// 
/// NEDEN [Authorize] ATTRIBUTE YOK:
/// Authorization, MediatR pipeline'da ISecuredRequest ile yapılıyor.
/// Controller sadece thin layer - request'i MediatR'a iletir.
/// 
/// ANTI-PATTERN:
/// Controller'da business logic yazmak.
/// "Sadece şu küçük validation'ı ekleyeyim" diye başlar,
/// 6 ay sonra 500 satırlık controller olur.
/// </summary>
public class AuthController : BaseApiController
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Password, GetIpAddress());
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.Username,
            request.Email,
            request.Password,
            request.DisplayName,
            GetIpAddress()
        );
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(Login), result);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(request.Token, GetIpAddress());
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        return NoContent();
    }
}

public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string Username, string Email, string Password, string? DisplayName);
public sealed record RefreshTokenRequest(string Token);
