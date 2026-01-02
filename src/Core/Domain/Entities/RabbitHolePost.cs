using Mootable.Domain.Common;
using Mootable.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Mootable.Domain.Entities
{
    /// <summary>
    /// Posts within a Rabbit Hole - Deep thoughts and discussions
    /// Matrix theme: "Transmissions from deep within the rabbit hole"
    /// </summary>
    public class RabbitHolePost : BaseEntity, IAuditableEntity
    {
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// The rabbit hole this post belongs to
        /// </summary>
        public Guid RabbitHoleId { get; set; }
        public RabbitHole RabbitHole { get; set; } = null!;

        /// <summary>
        /// Author of the post
        /// </summary>
        public Guid AuthorId { get; set; }
        public User Author { get; set; } = null!;

        /// <summary>
        /// Parent post for threaded discussions
        /// </summary>
        public Guid? ParentPostId { get; set; }
        public RabbitHolePost? ParentPost { get; set; }

        /// <summary>
        /// Child posts (replies)
        /// </summary>
        public ICollection<RabbitHolePost> Replies { get; set; } = new List<RabbitHolePost>();

        /// <summary>
        /// Depth score - how philosophical/deep this post is
        /// </summary>
        public int DepthScore { get; set; } = 0;

        /// <summary>
        /// Truth score - community validation of accuracy
        /// </summary>
        public int TruthScore { get; set; } = 0;

        /// <summary>
        /// Whether this post is pinned in the rabbit hole
        /// </summary>
        public bool IsPinned { get; set; } = false;

        /// <summary>
        /// Whether this post contains a "red pill" moment (paradigm-shifting insight)
        /// </summary>
        public bool IsRedPill { get; set; } = false;

        /// <summary>
        /// Tags associated with this post
        /// </summary>
        public string Tags { get; set; } = string.Empty;

        /// <summary>
        /// Media attachments (images, documents, etc.)
        /// </summary>
        public string? MediaUrls { get; set; } // JSON array of URLs

        /// <summary>
        /// Number of users who have "followed this rabbit" (engaged with the post)
        /// </summary>
        public int FollowerCount { get; set; } = 0;

        /// <summary>
        /// Post visibility
        /// </summary>
        public PostVisibility Visibility { get; set; } = PostVisibility.Public;

        /// <summary>
        /// Whether the post has been edited
        /// </summary>
        public bool IsEdited { get; set; } = false;

        /// <summary>
        /// Reactions to this post
        /// </summary>
        public ICollection<RabbitHolePostReaction> Reactions { get; set; } = new List<RabbitHolePostReaction>();

        // Audit properties
        public Guid CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}
