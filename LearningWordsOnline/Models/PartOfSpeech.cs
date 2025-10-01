using System.ComponentModel.DataAnnotations;

namespace LearningWordsOnline.Models
{
    public class PartOfSpeech
    {
        public int Id { get; set; }

        [Required]
        public int LanguageId { get; set; }

        public required Language Language { get; set; }

        [Required]
        public required string Name { get; set; }

        public virtual ICollection<Definition> Definitions { get; } = new List<Definition>();

    }
}
