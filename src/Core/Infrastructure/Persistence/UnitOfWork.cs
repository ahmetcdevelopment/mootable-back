using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Mootable.Application.Interfaces;
using Mootable.Domain.Common;
using Mootable.Domain.Entities;
using Mootable.Infrastructure.Persistence.Repositories;

namespace Mootable.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementation for managing database transactions and repository instances
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    // Repository instances
    private IRepository<User>? _users;
    private IRepository<Server>? _servers;
    private IRepository<MootTable>? _mootTables;
    private IRepository<RabbitHole>? _rabbitHoles;
    private IRepository<Message>? _messages;
    private IRepository<ServerMember>? _serverMembers;
    private IRepository<ServerRole>? _serverRoles;
    private IRepository<ServerMemberRole>? _serverMemberRoles;
    private IRepository<Role>? _roles;
    private IRepository<UserRole>? _userRoles;
    private IRepository<RefreshToken>? _refreshTokens;
    private IRepository<MessageReaction>? _messageReactions;
    private IRepository<MessageAttachment>? _messageAttachments;
    private IRepository<MootTableCategory>? _mootTableCategories;
    private IRepository<Post>? _posts;
    private IRepository<PostLike>? _postLikes;

    // Dictionary to store generic repositories
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    // Repository properties with lazy initialization
    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Server> Servers => _servers ??= new Repository<Server>(_context);
    public IRepository<MootTable> MootTables => _mootTables ??= new Repository<MootTable>(_context);
    public IRepository<RabbitHole> RabbitHoles => _rabbitHoles ??= new Repository<RabbitHole>(_context);
    public IRepository<Message> Messages => _messages ??= new Repository<Message>(_context);
    public IRepository<ServerMember> ServerMembers => _serverMembers ??= new Repository<ServerMember>(_context);
    public IRepository<ServerRole> ServerRoles => _serverRoles ??= new Repository<ServerRole>(_context);
    public IRepository<ServerMemberRole> ServerMemberRoles => _serverMemberRoles ??= new Repository<ServerMemberRole>(_context);
    public IRepository<Role> Roles => _roles ??= new Repository<Role>(_context);
    public IRepository<UserRole> UserRoles => _userRoles ??= new Repository<UserRole>(_context);
    public IRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new Repository<RefreshToken>(_context);
    public IRepository<MessageReaction> MessageReactions => _messageReactions ??= new Repository<MessageReaction>(_context);
    public IRepository<MessageAttachment> MessageAttachments => _messageAttachments ??= new Repository<MessageAttachment>(_context);
    public IRepository<MootTableCategory> MootTableCategories => _mootTableCategories ??= new Repository<MootTableCategory>(_context);
    public IRepository<Post> Posts => _posts ??= new Repository<Post>(_context);
    public IRepository<PostLike> PostLikes => _postLikes ??= new Repository<PostLike>(_context);

    public bool HasActiveTransaction => _currentTransaction != null;

    /// <summary>
    /// Generic repository factory method
    /// </summary>
    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        var type = typeof(T);

        if (!_repositories.ContainsKey(type))
        {
            _repositories[type] = new Repository<T>(_context);
        }

        return (IRepository<T>)_repositories[type];
    }

    /// <summary>
    /// Save changes to the database
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new Exception("A concurrency conflict occurred while saving changes.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new Exception("An error occurred while saving changes to the database.", ex);
        }
    }

    /// <summary>
    /// Begin a new database transaction
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commit the current transaction
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Dispose the Unit of Work
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _currentTransaction?.Dispose();
                _context.Dispose();
            }

            _disposed = true;
        }
    }
}