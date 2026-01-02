using Mootable.Domain.Common;
using System;

namespace Mootable.Domain.Entities
{
    /// <summary>
    /// Reactions to posts in Rabbit Holes
    /// Matrix theme: "Red pill/Blue pill reactions"
    /// </summary>
    public class RabbitHolePostReaction : BaseEntity
    {
        /// <summary>
        /// The post being reacted to
        /// </summary>
        public Guid PostId { get; set; }
        public RabbitHolePost Post { get; set; } = null!;

        /// <summary>
        /// The user reacting
        /// </summary>
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        /// <summary>
        /// Type of reaction
        /// Matrix themed: RedPill, BluePill, WhiteRabbit, GreenCode, Oracle, etc.
        /// </summary>
        public string ReactionType { get; set; } = string.Empty;

        /// <summary>
        /// When the reaction was made
        /// </summary>
        public DateTime ReactedAt { get; set; }
    }
}
