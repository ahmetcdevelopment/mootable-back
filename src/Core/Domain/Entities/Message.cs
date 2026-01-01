using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

public sealed class Message : BaseEntity, IAuditableEntity
{
    public string Content { get; set; } = default!;
    public bool IsEdited { get; set; }
    public bool IsPinned { get; set; }
    public MessageType Type { get; set; } = MessageType.Default;
    
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = default!;
    
    public Guid? MootTableId { get; set; }
    public MootTable? MootTable { get; set; }
    
    public Guid? RabbitHoleId { get; set; }
    public RabbitHole? RabbitHole { get; set; }
    
    public Guid? ReplyToId { get; set; }
    public Message? ReplyTo { get; set; }
    
    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // Navigation properties
    public ICollection<Message> Replies { get; set; } = new List<Message>();
    public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
}

public enum MessageType
{
    Default = 0,
    System = 1,
    RabbitHoleStarter = 2
}
