using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

public sealed class ServerRole : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Color { get; set; } = "#99AAB5";
    public int Position { get; set; }
    public ServerPermissions Permissions { get; set; }
    
    public Guid ServerId { get; set; }
    public Server Server { get; set; } = default!;
    
    public ICollection<ServerMemberRole> Members { get; set; } = new List<ServerMemberRole>();
}

[Flags]
public enum ServerPermissions : long
{
    None = 0,
    ViewMootTables = 1 << 0,
    SendMessages = 1 << 1,
    ManageMessages = 1 << 2,
    CreateMootTables = 1 << 3,
    ManageMootTables = 1 << 4,
    CreateRabbitHoles = 1 << 5,
    ManageRabbitHoles = 1 << 6,
    KickMembers = 1 << 7,
    BanMembers = 1 << 8,
    ManageRoles = 1 << 9,
    ManageServer = 1 << 10,
    Administrator = 1 << 11
}
