using Mootable.Domain.Common;

namespace Mootable.Domain.Entities;

public sealed class MessageAttachment : BaseEntity
{
    public string FileName { get; set; } = default!;
    public string FileUrl { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long FileSize { get; set; }
    
    public Guid MessageId { get; set; }
    public Message Message { get; set; } = default!;
}
