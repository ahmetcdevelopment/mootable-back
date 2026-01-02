using Mootable.Domain.Entities;

namespace Mootable.Application.Interfaces;

/// <summary>
/// Unit of Work pattern interface for managing transactions and repositories
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Repositories
    IRepository<User> Users { get; }
    IRepository<Server> Servers { get; }
    IRepository<MootTable> MootTables { get; }
    IRepository<RabbitHole> RabbitHoles { get; }
    IRepository<Message> Messages { get; }
    IRepository<ServerMember> ServerMembers { get; }
    IRepository<ServerRole> ServerRoles { get; }
    IRepository<ServerMemberRole> ServerMemberRoles { get; }
    IRepository<Role> Roles { get; }
    IRepository<UserRole> UserRoles { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<MessageReaction> MessageReactions { get; }
    IRepository<MessageAttachment> MessageAttachments { get; }
    IRepository<MootTableCategory> MootTableCategories { get; }

    // Transaction management
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    bool HasActiveTransaction { get; }

    // Generic repository factory for any entity type not listed above
    IRepository<T> Repository<T>() where T : Domain.Common.BaseEntity;
}