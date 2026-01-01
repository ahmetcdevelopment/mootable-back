using Mootable.Domain.Common;

namespace Mootable.Domain.Events;

public sealed class MemberJoinedServerEvent : BaseDomainEvent
{
    public Guid ServerId { get; }
    public Guid UserId { get; }
    
    public MemberJoinedServerEvent(Guid serverId, Guid userId)
    {
        ServerId = serverId;
        UserId = userId;
    }
}
