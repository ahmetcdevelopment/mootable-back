using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Mootable.Application.Pipelines.Caching;

/// <summary>
/// Query sonuçlarını cache'leyen behavior.
/// 
/// NEDEN REDIS (DISTRIBUTED CACHE):
/// In-memory cache, horizontal scale'de senkronizasyon problemi yaratır.
/// Server A'da cache var, Server B'de yok = inconsistent UX.
/// 
/// PRODUCTION DENEYİMİ:
/// 50K concurrent user'da in-memory cache kullanılan sistemde
/// "bazı kullanıcılar eski veriyi görüyor" bug'ı 2 hafta debug edildi.
/// Sebep: Sticky session olmayan load balancer + in-memory cache.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IDistributedCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (request is not ICachableRequest cachableRequest)
        {
            return await next();
        }

        var cacheKey = cachableRequest.CacheKey;
        var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedValue))
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<TResponse>(cachedValue)!;
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
        
        var response = await next();

        var options = new DistributedCacheEntryOptions();
        
        if (cachableRequest.SlidingExpiration.HasValue)
        {
            options.SlidingExpiration = cachableRequest.SlidingExpiration;
        }
        else
        {
            options.SlidingExpiration = TimeSpan.FromMinutes(5);
        }

        var serialized = JsonSerializer.Serialize(response);
        await _cache.SetStringAsync(cacheKey, serialized, options, cancellationToken);

        if (!string.IsNullOrEmpty(cachableRequest.CacheGroupKey))
        {
            await AddToGroupAsync(cachableRequest.CacheGroupKey, cacheKey, cancellationToken);
        }

        return response;
    }

    private async Task AddToGroupAsync(string groupKey, string cacheKey, CancellationToken cancellationToken)
    {
        var groupCacheKey = $"cache-group:{groupKey}";
        var existingKeys = await _cache.GetStringAsync(groupCacheKey, cancellationToken);
        
        var keys = string.IsNullOrEmpty(existingKeys) 
            ? new List<string>() 
            : JsonSerializer.Deserialize<List<string>>(existingKeys)!;
        
        if (!keys.Contains(cacheKey))
        {
            keys.Add(cacheKey);
            await _cache.SetStringAsync(
                groupCacheKey, 
                JsonSerializer.Serialize(keys), 
                new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(24) },
                cancellationToken);
        }
    }
}
