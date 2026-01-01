using Mootable.Domain.Common;

namespace Mootable.Domain.Events;

public sealed class MessageSentEvent : BaseDomainEvent
{
    public Guid MessageId { get; }
    public Guid AuthorId { get; }
    public Guid? MootTableId { get; }
    public Guid? RabbitHoleId { get; }
    
    public MessageSentEvent(Guid messageId, Guid authorId, Guid? mootTableId, Guid? rabbitHoleId)
    {
        MessageId = messageId;
        AuthorId = authorId;
        MootTableId = mootTableId;
        RabbitHoleId = rabbitHoleId;
    }
}
