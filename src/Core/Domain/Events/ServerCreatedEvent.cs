using Mootable.Domain.Common;

namespace Mootable.Domain.Events;

public sealed class ServerCreatedEvent : BaseDomainEvent
{
    public Guid ServerId { get; }
    public Guid OwnerId { get; }
    public string ServerName { get; }
    
    public ServerCreatedEvent(Guid serverId, Guid ownerId, string serverName)
    {
        ServerId = serverId;
        OwnerId = ownerId;
        ServerName = serverName;
    }
}
