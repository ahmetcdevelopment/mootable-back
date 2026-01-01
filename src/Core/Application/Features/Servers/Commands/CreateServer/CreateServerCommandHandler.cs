using MediatR;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using Mootable.Domain.Events;

namespace Mootable.Application.Features.Servers.Commands.CreateServer;

/// <summary>
/// Server oluşturma handler'ı.
/// 
/// İŞLEM SIRASI (TRANSACTION İÇİNDE):
/// 1. Server entity oluştur
/// 2. Default roller oluştur (Admin, Moderator, Member)
/// 3. Owner'ı member olarak ekle ve Admin rolü ver
/// 4. Default "general" MootTable oluştur
/// 
/// ANTI-PATTERN:
/// Bu işlemleri ayrı endpoint'lere bölmek.
/// Frontend'in 4 ayrı call yapması = partial state riski.
/// Network hatası olursa: Server var, role yok = kullanılamaz server.
/// </summary>
public sealed class CreateServerCommandHandler : IRequestHandler<CreateServerCommand, CreateServerResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateServerCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CreateServerResponse> Handle(CreateServerCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;
        var now = DateTime.UtcNow;

        var server = new Server
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            IsPublic = request.IsPublic,
            OwnerId = userId,
            InviteCode = GenerateInviteCode(),
            CreatedAt = now,
            CreatedBy = userId
        };

        var adminRole = new ServerRole
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            ServerId = server.Id,
            Position = 0,
            Color = "#E74C3C",
            Permissions = ServerPermissions.Administrator,
            CreatedAt = now
        };

        var moderatorRole = new ServerRole
        {
            Id = Guid.NewGuid(),
            Name = "Moderator",
            ServerId = server.Id,
            Position = 1,
            Color = "#3498DB",
            Permissions = ServerPermissions.ViewMootTables | ServerPermissions.SendMessages | 
                         ServerPermissions.ManageMessages | ServerPermissions.KickMembers,
            CreatedAt = now
        };

        var memberRole = new ServerRole
        {
            Id = Guid.NewGuid(),
            Name = "Member",
            ServerId = server.Id,
            Position = 2,
            Color = "#99AAB5",
            Permissions = ServerPermissions.ViewMootTables | ServerPermissions.SendMessages | 
                         ServerPermissions.CreateRabbitHoles,
            CreatedAt = now
        };

        var ownerMember = new ServerMember
        {
            Id = Guid.NewGuid(),
            ServerId = server.Id,
            UserId = userId,
            JoinedAt = now,
            CreatedAt = now
        };

        ownerMember.Roles.Add(new ServerMemberRole
        {
            Id = Guid.NewGuid(),
            ServerMemberId = ownerMember.Id,
            ServerRoleId = adminRole.Id,
            CreatedAt = now
        });

        var generalMootTable = new MootTable
        {
            Id = Guid.NewGuid(),
            Name = "general",
            Topic = "General discussion",
            ServerId = server.Id,
            Position = 0,
            Type = MootTableType.Text,
            CreatedAt = now,
            CreatedBy = userId
        };

        server.AddDomainEvent(new ServerCreatedEvent(server.Id, userId, server.Name));

        _context.Servers.Add(server);
        _context.ServerRoles.Add(adminRole);
        _context.ServerRoles.Add(moderatorRole);
        _context.ServerRoles.Add(memberRole);
        _context.ServerMembers.Add(ownerMember);
        _context.MootTables.Add(generalMootTable);

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateServerResponse(
            ServerId: server.Id,
            Name: server.Name,
            Description: server.Description,
            InviteCode: server.InviteCode,
            IsPublic: server.IsPublic,
            CreatedAt: server.CreatedAt
        );
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
