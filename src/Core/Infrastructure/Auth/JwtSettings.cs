namespace Mootable.Infrastructure.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";
    
    public string Secret { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
