using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

/// <summary>
/// Post'lara verilen like'ları tutan entity.
/// Matrix temasında "Red Pill" veya "Blue Pill" olarak da adlandırılabilir.
/// </summary>
public class PostLike : BaseEntity, IAuditableEntity
{
    // IAuditableEntity implementation
    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Like'ı atan kullanıcı
    /// </summary>
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Like atılan post
    /// </summary>
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    /// <summary>
    /// Like türü (gelecekte farklı reaksiyon tipleri için)
    /// Örnek: RedPill, BluePill, Enlightened, Awakened
    /// </summary>
    public string LikeType { get; set; } = "RedPill";
}