using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

/// <summary>
/// MootTable = Tartışma Masası
/// Her MootTable bir tartışma konusu etrafında döner.
/// Rabbit Hole'lar bu tartışmalardan türeyen alt dallanmalardır.
/// </summary>
public sealed class MootTable : BaseEntity, IAuditableEntity
{
    public string Name { get; set; } = default!;
    public string? Topic { get; set; }
    public int Position { get; set; }
    public MootTableType Type { get; set; } = MootTableType.Text;
    public bool IsArchived { get; set; }
    
    public Guid ServerId { get; set; }
    public Server Server { get; set; } = default!;
    
    public Guid? CategoryId { get; set; }
    public MootTableCategory? Category { get; set; }
    
    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // Navigation properties
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<RabbitHole> RabbitHoles { get; set; } = new List<RabbitHole>();
}

public enum MootTableType
{
    Text = 0,
    Voice = 1,
    Announcement = 2
}
