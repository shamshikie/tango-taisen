using System.ComponentModel.DataAnnotations;

namespace LearningWordsOnline.Models
{
    public class AppUserDefinition
    {
        [Required]
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        [Required]
        public int DefinitionId { get; set; }
        public Definition Definition { get; set; } = null!;

        [Required]
        public int Count { get; set; } = 0;
        [Required]
        public int WrongCount { get; set; } = 0;

        [Required]
        public DateTime AnsweredAt { get; set; }
    }
}
