using Mootable.Domain.Common;
using System;
using System.Collections.Generic;

namespace Mootable.Domain.Entities
{
    /// <summary>
    /// Rabbit Hole - Topic-specific channels for deep dives into subjects
    /// Matrix theme: "Follow the white rabbit" - channels that lead to deeper knowledge
    /// </summary>
    public class RabbitHole : BaseEntity, IAuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty; // e.g., "Philosophy", "Technology", "Sports"
        public string Slug { get; set; } = string.Empty; // URL-friendly identifier

        /// <summary>
        /// Depth level - how deep this rabbit hole goes
        /// 1 = Surface level, 5 = Deep philosophical discussions
        /// </summary>
        public int DepthLevel { get; set; } = 1;

        /// <summary>
        /// Rabbit Hole color theme (Matrix green variations)
        /// </summary>
        public string ColorHex { get; set; } = "#00FF41"; // Matrix green

        /// <summary>
        /// Icon or emoji representing this rabbit hole
        /// </summary>
        public string Icon { get; set; } = "🐇";

        /// <summary>
        /// Parent rabbit hole for sub-topics
        /// </summary>
        public Guid? ParentId { get; set; }
        public RabbitHole? Parent { get; set; }

        /// <summary>
        /// Child rabbit holes (sub-topics)
        /// </summary>
        public ICollection<RabbitHole> SubHoles { get; set; } = new List<RabbitHole>();

        /// <summary>
        /// Messages posted in this rabbit hole
        /// </summary>
        public ICollection<RabbitHolePost> Posts { get; set; } = new List<RabbitHolePost>();

        /// <summary>
        /// Users following this rabbit hole
        /// </summary>
        public ICollection<RabbitHoleFollower> Followers { get; set; } = new List<RabbitHoleFollower>();

        /// <summary>
        /// Tags associated with this rabbit hole
        /// </summary>
        public string Tags { get; set; } = string.Empty; // Comma-separated tags

        /// <summary>
        /// Whether this rabbit hole is public or requires special access
        /// </summary>
        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// Minimum enlightenment score required to post
        /// </summary>
        public int MinimumEnlightenmentScore { get; set; } = 0;

        /// <summary>
        /// Number of active explorers (users currently viewing)
        /// </summary>
        public int ActiveExplorers { get; set; } = 0;

        /// <summary>
        /// Total number of posts in this rabbit hole
        /// </summary>
        public int PostCount { get; set; } = 0;

        /// <summary>
        /// Pinned message for important information
        /// </summary>
        public string? PinnedMessage { get; set; }

        // Audit properties
        public Guid CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}
