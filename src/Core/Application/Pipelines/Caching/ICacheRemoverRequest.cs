namespace Mootable.Application.Pipelines.Caching;

/// <summary>
/// Command sonrası cache invalidation için interface.
/// 
/// PRODUCTION DENEYİMİ:
/// Discord benzeri bir sistemde en sık görülen bug:
/// "Mesaj gönderdim ama listede görünmüyor" (cache invalidation eksik)
/// 
/// Bu yapıda her Command, hangi cache'leri invalidate edeceğini
/// explicit olarak bildirir. Unutma riski sıfır.
/// </summary>
public interface ICacheRemoverRequest
{
    string[]? CacheKeysToRemove { get; }
    string[]? CacheGroupKeysToRemove { get; }
}
