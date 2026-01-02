using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Mootable.IntegrationTests.Fixtures;

/// <summary>
/// Test authentication handler for bypassing JWT in tests
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if we have a test user header
        if (!Request.Headers.ContainsKey("X-Test-User"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var testUserId = Request.Headers["X-Test-User"].ToString();
        var testUsername = Request.Headers["X-Test-Username"].ToString();
        var testRoles = Request.Headers["X-Test-Roles"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);

        var claims = new List<Claim>
        {
            new Claim("userId", testUserId),
            new Claim(ClaimTypes.NameIdentifier, testUserId),
            new Claim(ClaimTypes.Name, testUsername ?? "TestUser"),
            new Claim("username", testUsername ?? "TestUser"),
            new Claim("email", $"{testUsername ?? "test"}@test.com")
        };

        foreach (var role in testRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
}