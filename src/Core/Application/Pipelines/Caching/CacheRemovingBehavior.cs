using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Mootable.Application.Pipelines.Caching;

/// <summary>
/// Command sonrası cache invalidation yapan behavior.
/// 
/// SIRALAMA KRİTİK:
/// Bu behavior, handler SONRASINDA çalışmalı.
/// Handler başarısız olursa cache invalidation yapılmamalı.
/// 
/// ANTI-PATTERN:
/// Handler içinde manuel cache.Remove() çağrısı yapmak.
/// Transaction rollback durumunda cache zaten invalidate edilmiş olur = stale data.
/// </summary>
public sealed class CacheRemovingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheRemovingBehavior<TRequest, TResponse>> _logger;

    public CacheRemovingBehavior(IDistributedCache cache, ILogger<CacheRemovingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is not ICacheRemoverRequest cacheRemoverRequest)
        {
            return response;
        }

        if (cacheRemoverRequest.CacheKeysToRemove != null)
        {
            foreach (var key in cacheRemoverRequest.CacheKeysToRemove)
            {
                await _cache.RemoveAsync(key, cancellationToken);
                _logger.LogDebug("Removed cache key: {CacheKey}", key);
            }
        }

        if (cacheRemoverRequest.CacheGroupKeysToRemove != null)
        {
            foreach (var groupKey in cacheRemoverRequest.CacheGroupKeysToRemove)
            {
                await RemoveGroupAsync(groupKey, cancellationToken);
            }
        }

        return response;
    }

    private async Task RemoveGroupAsync(string groupKey, CancellationToken cancellationToken)
    {
        var groupCacheKey = $"cache-group:{groupKey}";
        var existingKeys = await _cache.GetStringAsync(groupCacheKey, cancellationToken);

        if (string.IsNullOrEmpty(existingKeys))
        {
            return;
        }

        var keys = JsonSerializer.Deserialize<List<string>>(existingKeys)!;
        
        foreach (var key in keys)
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Removed cache key from group {GroupKey}: {CacheKey}", groupKey, key);
        }

        await _cache.RemoveAsync(groupCacheKey, cancellationToken);
        _logger.LogDebug("Removed cache group: {GroupKey}", groupKey);
    }
}
