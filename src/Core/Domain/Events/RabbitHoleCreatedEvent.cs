using Mootable.Domain.Common;

namespace Mootable.Domain.Events;

public sealed class RabbitHoleCreatedEvent : BaseDomainEvent
{
    public Guid RabbitHoleId { get; }
    public Guid MootTableId { get; }
    public Guid StarterMessageId { get; }
    public Guid CreatedByUserId { get; }
    
    public RabbitHoleCreatedEvent(Guid rabbitHoleId, Guid mootTableId, Guid starterMessageId, Guid createdByUserId)
    {
        RabbitHoleId = rabbitHoleId;
        MootTableId = mootTableId;
        StarterMessageId = starterMessageId;
        CreatedByUserId = createdByUserId;
    }
}
