using System.ComponentModel.DataAnnotations;

namespace LearningWordsOnline.Models
{
    public class Battle
    {
        public int Id { get; set; }

        [Required]
        public int LanguageId { get; set; }
        public Language Language { get; set; } = null!;

        [Required]
        public DateTime BattledAt { get; set; }

        public virtual ICollection<BattleAppUser> BattleAppUsers { get; } = new List<BattleAppUser>();
    }
}
