using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

public sealed class ServerMemberRole : BaseEntity
{
    public Guid ServerMemberId { get; set; }
    public ServerMember ServerMember { get; set; } = default!;
    
    public Guid ServerRoleId { get; set; }
    public ServerRole ServerRole { get; set; } = default!;
}
