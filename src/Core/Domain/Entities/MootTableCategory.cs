using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

public sealed class MootTableCategory : BaseEntity
{
    public string Name { get; set; } = default!;
    public int Position { get; set; }
    
    public Guid ServerId { get; set; }
    public Server Server { get; set; } = default!;
    
    public ICollection<MootTable> MootTables { get; set; } = new List<MootTable>();
}
