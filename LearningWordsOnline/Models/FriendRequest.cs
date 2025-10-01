using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningWordsOnline.Models
{
    public class FriendRequest
    {
        public int Id { get; set; }

        /// <summary>
        /// SenderId
        /// </summary>
        [Required]
        public int AppUserId1 { get; set; }

        [ForeignKey("AppUserId1")]
        [InverseProperty("SentRequests")]  // AppUser の SentRequests と関連
        public AppUser Sender { get; set; } = null!;

        /// <summary>
        /// ReceiverId
        /// </summary>
        [Required]
        public int AppUserId2 { get; set; }

        [ForeignKey("AppUserId2")]
        [InverseProperty("ReceivedRequests")]  // AppUser の ReceivedRequests と関連
        public AppUser Receiver { get; set; } = null!;

        [Required]
        public FriendRequestStatus FriendRequestStatus { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? RespondedAt { get; set; } = null;

        public DateTime? ReferencedAt { get; set; } = null;
    }
}
