using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mootable.Application.Features.Auth.Commands.ExternalLogin;
using System.Security.Claims;

namespace Mootable.WebAPI.Controllers;

/// <summary>
/// OAuth2 authentication controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class OAuth2Controller : BaseApiController
{
    private readonly IConfiguration _configuration;

    public OAuth2Controller(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Initiate OAuth2 login flow
    /// </summary>
    /// <param name="provider">Provider name (Google, Microsoft)</param>
    /// <param name="returnUrl">URL to redirect after login</param>
    [HttpGet("login/{provider}")]
    [AllowAnonymous]
    public IActionResult Login(string provider, string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = _configuration["Cors:AllowedOrigins"]?.Split(',').FirstOrDefault() ?? "http://localhost:3000";
        }

        var authenticationScheme = provider.ToLower() switch
        {
            "google" => GoogleDefaults.AuthenticationScheme,
            "microsoft" => MicrosoftAccountDefaults.AuthenticationScheme,
            _ => throw new ArgumentException($"Unknown provider: {provider}")
        };

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(Callback), new { provider, returnUrl }),
            Items = { ["provider"] = provider, ["returnUrl"] = returnUrl }
        };

        return Challenge(properties, authenticationScheme);
    }

    /// <summary>
    /// OAuth2 callback endpoint
    /// </summary>
    [HttpGet("callback/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(string provider, string? returnUrl = null)
    {
        var result = await HttpContext.AuthenticateAsync();

        if (!result.Succeeded)
        {
            // Redirect to frontend with error
            returnUrl = returnUrl ?? _configuration["Cors:AllowedOrigins"]?.Split(',').FirstOrDefault() ?? "http://localhost:3000";
            return Redirect($"{returnUrl}/login?error=oauth_failed&provider={provider}");
        }

        // Extract user information from claims
        var claims = result.Principal!.Claims;
        var providerKey = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var photoUrl = claims.FirstOrDefault(c => c.Type == "picture" || c.Type == "urn:google:picture")?.Value;

        // Get tokens if available
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
        var expiresAt = await HttpContext.GetTokenAsync("expires_at");

        DateTime? tokenExpiresAt = null;
        if (!string.IsNullOrEmpty(expiresAt) && DateTime.TryParse(expiresAt, out var parsedExpiry))
        {
            tokenExpiresAt = parsedExpiry;
        }

        // Get client IP
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Process external login
        var command = new ExternalLoginCommand
        {
            Provider = provider,
            ProviderKey = providerKey!,
            Email = email!,
            DisplayName = name,
            PhotoUrl = photoUrl,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiresAt = tokenExpiresAt,
            IpAddress = ipAddress
        };

        var response = await Mediator.Send(command);

        // Sign out from the external provider
        await HttpContext.SignOutAsync();

        // Redirect to frontend with tokens
        returnUrl = returnUrl ?? _configuration["Cors:AllowedOrigins"]?.Split(',').FirstOrDefault() ?? "http://localhost:3000";
        var queryParams = $"?access_token={response.AccessToken}" +
                         $"&refresh_token={response.RefreshToken}" +
                         $"&user_id={response.UserId}" +
                         $"&username={Uri.EscapeDataString(response.Username)}" +
                         $"&email={Uri.EscapeDataString(response.Email)}" +
                         $"&is_new_user={response.IsNewUser.ToString().ToLower()}" +
                         $"&provider={provider}";

        return Redirect($"{returnUrl}/oauth/callback{queryParams}");
    }

    /// <summary>
    /// Link existing account with OAuth2 provider
    /// </summary>
    [HttpPost("link/{provider}")]
    [Authorize]
    public IActionResult LinkAccount(string provider)
    {
        var authenticationScheme = provider.ToLower() switch
        {
            "google" => GoogleDefaults.AuthenticationScheme,
            "microsoft" => MicrosoftAccountDefaults.AuthenticationScheme,
            _ => throw new ArgumentException($"Unknown provider: {provider}")
        };

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(LinkCallback), new { provider }),
            Items = { ["provider"] = provider }
        };

        return Challenge(properties, authenticationScheme);
    }

    /// <summary>
    /// OAuth2 link callback endpoint
    /// </summary>
    [HttpGet("link-callback/{provider}")]
    [Authorize]
    public async Task<IActionResult> LinkCallback(string provider)
    {
        // TODO: Implement account linking logic
        // This would link the OAuth2 provider to the currently logged-in user
        return Ok(new { message = "Account linked successfully", provider });
    }

    /// <summary>
    /// Get OAuth2 providers configuration
    /// </summary>
    [HttpGet("providers")]
    [AllowAnonymous]
    public IActionResult GetProviders()
    {
        var providers = new List<object>();

        if (_configuration["OAuth2:Google:ClientId"] != "YOUR_GOOGLE_CLIENT_ID")
        {
            providers.Add(new
            {
                name = "Google",
                enabled = true,
                loginUrl = Url.Action(nameof(Login), new { provider = "Google" })
            });
        }

        if (_configuration["OAuth2:Microsoft:ClientId"] != "YOUR_MICROSOFT_CLIENT_ID")
        {
            providers.Add(new
            {
                name = "Microsoft",
                enabled = true,
                loginUrl = Url.Action(nameof(Login), new { provider = "Microsoft" })
            });
        }

        return Ok(providers);
    }
}