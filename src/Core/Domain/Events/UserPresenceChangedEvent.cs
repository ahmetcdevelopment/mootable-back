using Mootable.Domain.Common;
using Mootable.Domain.Entities;

namespace Mootable.Domain.Events;

public sealed class UserPresenceChangedEvent : BaseDomainEvent
{
    public Guid UserId { get; }
    public UserStatus OldStatus { get; }
    public UserStatus NewStatus { get; }
    
    public UserPresenceChangedEvent(Guid userId, UserStatus oldStatus, UserStatus newStatus)
    {
        UserId = userId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}
