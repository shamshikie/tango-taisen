using System.ComponentModel.DataAnnotations;

namespace LearningWordsOnline.Models
{
    public class BattleAppUser
    {
        [Required]
        public int BattleId { get; set; }
        public Battle Battle { get; set; } = null!;

        [Required]
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        [Required]
        public required int Position { get; set; } = -1;
    }
}
