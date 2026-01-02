using MediatR;
using Microsoft.Extensions.Logging;
using Mootable.Application.Interfaces;

namespace Mootable.Application.Pipelines.Transaction;

/// <summary>
/// Transaction management behavior using Unit of Work pattern.
///
/// PRODUCTION EXPERIENCE:
/// "Partial update" bug: Server created but default role not created.
/// Reason: No rollback after exception.
///
/// This behavior ensures:
/// 1. Transaction starts before handler
/// 2. Handler success = commit
/// 3. Handler exception = rollback
///
/// ANTI-PATTERN:
/// Opening manual transactions inside handlers.
/// Nested transactions, savepoints, deadlock = debug nightmare.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IUnitOfWork unitOfWork, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
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

        if (_unitOfWork.HasActiveTransaction)
        {
            _logger.LogDebug("Transaction already exists for {RequestName}, using existing", requestName);
            return await next();
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            _logger.LogDebug("Started transaction for {RequestName}", requestName);

            var response = await next();

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogDebug("Committed transaction for {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rolling back transaction for {RequestName}", requestName);

            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            throw;
        }
    }
}
