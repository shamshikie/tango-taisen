using System.ComponentModel.DataAnnotations;

namespace LearningWordsOnline.Models
{
    public class Word
    {
        public int Id { get; set; }

        [Required]
        public int LanguageId { get; set; }

        public required Language Language { get; set; }

        [Required]
        public required string Spelling { get; set; }

        public int? Level { get; set; }

        public virtual ICollection<Pronunciation> Pronunciations { get; } = new List<Pronunciation>();
        public virtual ICollection<Definition> Definitions { get; } = new List<Definition>();

    }
}
