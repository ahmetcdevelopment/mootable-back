using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Mootable.Application.Pipelines.Transaction;

/// <summary>
/// EF Core transaction yönetimi için behavior.
/// 
/// PRODUCTION DENEYİMİ:
/// "Partial update" bug'ı: Server oluşturuldu ama default role oluşturulmadı.
/// Sebep: Exception sonrası rollback yok.
/// 
/// Bu behavior ile:
/// 1. Handler başlamadan transaction açılır
/// 2. Handler başarılı = commit
/// 3. Handler exception = rollback
/// 
/// ANTI-PATTERN:
/// Handler içinde manual transaction açmak.
/// Nested transaction'lar, savepoint'ler, deadlock = debug nightmare.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(DbContext dbContext, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;

        if (_dbContext.Database.CurrentTransaction != null)
        {
            _logger.LogDebug("Transaction already exists for {RequestName}, using existing", requestName);
            return await next();
        }

        IDbContextTransaction? transaction = null;
        
        try
        {
            transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            _logger.LogDebug("Started transaction for {RequestName}", requestName);

            var response = await next();

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            _logger.LogDebug("Committed transaction for {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rolling back transaction for {RequestName}", requestName);
            
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }
}
