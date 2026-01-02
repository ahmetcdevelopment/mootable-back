using Mootable.Domain.Common;
using Mootable.Domain.Enums;

namespace Mootable.Domain.Entities;

/// <summary>
/// Wonderland'deki post entity'si.
/// Matrix'ten çıkış temasına uygun, kullanıcıların düşüncelerini paylaştığı yapı.
/// </summary>
public class Post : BaseEntity, IAuditableEntity
{
    // IAuditableEntity implementation
    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Post'un HTML formatında içeriği (rich text için)
    /// </summary>
    public string? HtmlContent { get; set; }

    /// <summary>
    /// Post'un ait olduğu kategori/ilgi alanı (futbol, siyaset, teknoloji vs)
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Post'un hashtag'leri (aramada kullanılacak)
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Post'a eklenen medya dosyaları (resim, video URL'leri)
    /// </summary>
    public List<string> MediaUrls { get; set; } = new();

    /// <summary>
    /// Post görünürlüğü
    /// </summary>
    public PostVisibility Visibility { get; set; } = PostVisibility.Public;

    /// <summary>
    /// Eğer bu bir reply/yorum ise, parent post'un ID'si
    /// </summary>
    public Guid? ParentPostId { get; set; }
    public Post? ParentPost { get; set; }

    /// <summary>
    /// Bu post'a yapılan reply'lar
    /// </summary>
    public List<Post> Replies { get; set; } = new();

    /// <summary>
    /// Post'u oluşturan kullanıcı
    /// </summary>
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Post'a verilen like'lar
    /// </summary>
    public List<PostLike> Likes { get; set; } = new();

    /// <summary>
    /// Post'un aldığı like sayısı (cache için)
    /// </summary>
    public int LikeCount { get; set; }

    /// <summary>
    /// Post'un aldığı reply sayısı (cache için)
    /// </summary>
    public int ReplyCount { get; set; }

    /// <summary>
    /// Post'un kaç kez görüntülendiği
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Post'un kaç kez paylaşıldığı (repost)
    /// </summary>
    public int ShareCount { get; set; }

    /// <summary>
    /// Matrix'ten çıkış seviyesi (kullanıcı engagement'ına göre)
    /// </summary>
    public int EnlightenmentScore { get; set; }

    /// <summary>
    /// Post'un spam/zararlı içerik olarak işaretlenme sayısı
    /// </summary>
    public int ReportCount { get; set; }

    /// <summary>
    /// Post'un moderasyon durumu
    /// </summary>
    public bool IsModerated { get; set; }

    /// <summary>
    /// Post'un pin'lenip pin'lenmediği (kullanıcı profilinde)
    /// </summary>
    public bool IsPinned { get; set; }
}