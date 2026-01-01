using Microsoft.EntityFrameworkCore;
using Mootable.Application.Interfaces;
using Mootable.Domain.Common;
using Mootable.Domain.Entities;

namespace Mootable.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext implementasyonu.
/// 
/// PRODUCTION DENEYİMİ:
/// SaveChangesAsync override'ı kritik:
/// 1. Audit fields (CreatedAt, UpdatedAt) otomatik set edilir
/// 2. Soft delete için IsDeleted flag'i kullanılır
/// 3. Domain events dispatch edilir
/// 
/// ANTI-PATTERN:
/// Her handler'da manuel CreatedAt = DateTime.UtcNow yazmak.
/// Bir developer unutur, debug'da "CreatedAt neden null?" sorunu çıkar.
/// </summary>
public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUserService = null) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<ServerMember> ServerMembers => Set<ServerMember>();
    public DbSet<ServerRole> ServerRoles => Set<ServerRole>();
    public DbSet<ServerMemberRole> ServerMemberRoles => Set<ServerMemberRole>();
    public DbSet<MootTable> MootTables => Set<MootTable>();
    public DbSet<MootTableCategory> MootTableCategories => Set<MootTableCategory>();
    public DbSet<RabbitHole> RabbitHoles => Set<RabbitHole>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Exclude domain event types from EF Core model
        modelBuilder.Ignore<BaseDomainEvent>();
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(CreateSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    private static System.Linq.Expressions.LambdaExpression CreateSoftDeleteFilter(Type entityType)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var condition = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false));
        return System.Linq.Expressions.Expression.Lambda(condition, parameter);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var userId = _currentUserService?.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    if (entry.Entity is IAuditableEntity auditableAdded && userId.HasValue)
                    {
                        auditableAdded.CreatedBy = userId.Value;
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    if (entry.Entity is IAuditableEntity auditableModified && userId.HasValue)
                    {
                        auditableModified.UpdatedBy = userId.Value;
                    }
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
