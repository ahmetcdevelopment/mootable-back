using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

/// <summary>
/// Server = Nebuchadnezzar (Matrix metaforu)
/// Her server bir "gemi" gibi düşünülmeli - içinde tartışma masaları (MootTable) barındırır.
/// </summary>
public sealed class Server : BaseEntity, IAuditableEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string InviteCode { get; set; } = default!;
    public bool IsPublic { get; set; }
    
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = default!;
    
    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // Navigation properties
    public ICollection<ServerMember> Members { get; set; } = new List<ServerMember>();
    public ICollection<MootTable> MootTables { get; set; } = new List<MootTable>();
    public ICollection<ServerRole> ServerRoles { get; set; } = new List<ServerRole>();
}
