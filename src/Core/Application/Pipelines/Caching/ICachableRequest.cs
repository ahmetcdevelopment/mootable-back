namespace Mootable.Application.Pipelines.Caching;

/// <summary>
/// Query'ler için cache interface'i.
/// 
/// CacheKey: Redis'te unique identifier (örn: "moot-table:123")
/// CacheGroupKey: İlişkili cache'leri gruplamak için (örn: "server:456:moot-tables")
/// SlidingExpiration: Her erişimde TTL yenilenir
/// 
/// ANTI-PATTERN UYARISI:
/// Cache key'leri string concatenation ile oluşturmak (örn: "user-" + userId)
/// production'da debug nightmare'ına dönüşür.
/// Key format'ı request class'ında tanımlı olmalı, handler'da değil.
/// </summary>
public interface ICachableRequest
{
    string CacheKey { get; }
    string? CacheGroupKey { get; }
    TimeSpan? SlidingExpiration { get; }
}
