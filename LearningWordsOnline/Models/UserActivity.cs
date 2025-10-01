using System.ComponentModel.DataAnnotations;

namespace LearningWordsOnline.Models
{
    public class UserActivity
    {
        public int Id { get; set; }

        [Required]
        public required int AppUserId { get; set; }

        [Required]
        public required DateTime LastLoginedAt { get; set; }
    }
}
