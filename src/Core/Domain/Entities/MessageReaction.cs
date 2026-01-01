using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

public sealed class MessageReaction : BaseEntity
{
    public string Emoji { get; set; } = default!;
    
    public Guid MessageId { get; set; }
    public Message Message { get; set; } = default!;
    
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
}
