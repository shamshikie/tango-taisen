using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningWordsOnline.Models
{
    public class RoomInvitation
    {
        public int Id { get; set; }

        [Required]
        public required string RoomId { get; set; }

        [Required]
        public required int AppUserId1 { get; set; }

        [ForeignKey("AppUserId1")]
        [InverseProperty("SentInvitations")]  // AppUser の SentInvitation と紐づけ
        public AppUser Inviter { get; set; } = null!;

        [Required]
        public required int AppUserId2 { get; set; }

        [ForeignKey("AppUserId2")]
        [InverseProperty("ReceivedInvitations")]  // AppUser の ReceivedInvitation と紐づけ
        public AppUser Invitee { get; set; } = null!;

        [Required]
        public required DateTime CreatedAt { get; set; }

        public DateTime? DismissedAt { get; set; } = null;

        public DateTime? ReferencedAt { get; set; } = null;
    }
}
