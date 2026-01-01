using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public string CreatedByIp { get; set; } = default!;
    public string? RevokedByIp { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
