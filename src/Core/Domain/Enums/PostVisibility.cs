namespace Mootable.Domain.Enums;

/// <summary>
/// Post görünürlük seçenekleri
/// </summary>
public enum PostVisibility
{
    /// <summary>
    /// Herkes görebilir
    /// </summary>
    Public = 0,

    /// <summary>
    /// Sadece takipçiler görebilir
    /// </summary>
    FollowersOnly = 1,

    /// <summary>
    /// Sadece arkadaşlar görebilir
    /// </summary>
    FriendsOnly = 2,

    /// <summary>
    /// Sadece post sahibi görebilir (taslak)
    /// </summary>
    Private = 3,

    /// <summary>
    /// Sadece belirli bir sunucu/grup görebilir
    /// </summary>
    ServerOnly = 4
}