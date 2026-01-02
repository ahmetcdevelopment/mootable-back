using Microsoft.EntityFrameworkCore;
using Mootable.Domain.Entities;

namespace Mootable.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Server> Servers { get; }
    DbSet<ServerMember> ServerMembers { get; }
    DbSet<ServerRole> ServerRoles { get; }
    DbSet<ServerMemberRole> ServerMemberRoles { get; }
    DbSet<MootTable> MootTables { get; }
    DbSet<MootTableCategory> MootTableCategories { get; }
    DbSet<RabbitHole> RabbitHoles { get; }
    DbSet<RabbitHolePost> RabbitHolePosts { get; }
    DbSet<RabbitHoleFollower> RabbitHoleFollowers { get; }
    DbSet<RabbitHolePostReaction> RabbitHolePostReactions { get; }
    DbSet<Message> Messages { get; }
    DbSet<MessageAttachment> MessageAttachments { get; }
    DbSet<MessageReaction> MessageReactions { get; }
    DbSet<Post> Posts { get; }
    DbSet<PostLike> PostLikes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
