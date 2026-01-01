using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

public sealed class ServerMember : BaseEntity
{
    public Guid ServerId { get; set; }
    public Server Server { get; set; } = default!;
    
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    
    public string? Nickname { get; set; }
    public DateTime JoinedAt { get; set; }
    
    public ICollection<ServerMemberRole> Roles { get; set; } = new List<ServerMemberRole>();
}
