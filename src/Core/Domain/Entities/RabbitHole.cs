using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

/// <summary>
/// RabbitHole = Tavşan Deliği (Matrix metaforu)
/// Bir tartışmadan türeyen alt tartışma thread'i.
/// "Follow the white rabbit" - ana konudan sapan, derinleşen tartışmalar için.
/// </summary>
public sealed class RabbitHole : BaseEntity, IAuditableEntity
{
    public string Title { get; set; } = default!;
    public bool IsResolved { get; set; }
    public bool IsLocked { get; set; }
    
    public Guid MootTableId { get; set; }
    public MootTable MootTable { get; set; } = default!;
    
    public Guid StarterMessageId { get; set; }
    public Message StarterMessage { get; set; } = default!;
    
    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // Navigation properties
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
