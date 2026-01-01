using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

public sealed class Role : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
