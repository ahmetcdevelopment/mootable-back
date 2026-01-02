using Mootable.Domain.Common;
using System;

namespace Mootable.Domain.Entities
{
    /// <summary>
    /// Users following a Rabbit Hole
    /// Matrix theme: "Those who follow the white rabbit"
    /// </summary>
    public class RabbitHoleFollower : BaseEntity
    {
        /// <summary>
        /// The rabbit hole being followed
        /// </summary>
        public Guid RabbitHoleId { get; set; }
        public RabbitHole RabbitHole { get; set; } = null!;

        /// <summary>
        /// The user following the rabbit hole
        /// </summary>
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        /// <summary>
        /// When the user started following
        /// </summary>
        public DateTime FollowedAt { get; set; }

        /// <summary>
        /// Notification preference for new posts
        /// </summary>
        public bool NotifyOnNewPosts { get; set; } = true;

        /// <summary>
        /// Notification preference for trending discussions
        /// </summary>
        public bool NotifyOnTrending { get; set; } = false;

        /// <summary>
        /// User engagement level with this rabbit hole (0-100)
        /// </summary>
        public int EngagementLevel { get; set; } = 0;

        /// <summary>
        /// Last time the user visited this rabbit hole
        /// </summary>
        public DateTime? LastVisitedAt { get; set; }
    }
}
