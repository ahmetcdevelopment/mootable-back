using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

public sealed class User : BaseEntity, IAuditableEntity
{
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Offline;
    public DateTime? LastSeenAt { get; set; }
    
    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // Navigation properties
    public ICollection<ServerMember> ServerMemberships { get; set; } = new List<ServerMember>();
    public ICollection<Server> OwnedServers { get; set; } = new List<Server>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public enum UserStatus
{
    Online = 0,
    Away = 1,
    DoNotDisturb = 2,
    Offline = 3
}
