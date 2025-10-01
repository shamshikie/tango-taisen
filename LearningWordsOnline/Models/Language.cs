using LearningWordsOnline.Models;
using System.ComponentModel.DataAnnotations;

namespace LearningWordsOnline.Models
{
    public class Language
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(8)]
        //NOTE: ISO639-1で運用
        public required string Code { get; set; }

        [Required]
        [MaxLength(32)]
        public required string Name { get; set; }

        public virtual ICollection<Category> Categories { get; } = new List<Category>();
        public virtual ICollection<Battle> Battles { get; } = new List<Battle>();
        public virtual ICollection<Word> Words { get; } = new List<Word>();
        public virtual ICollection<PartOfSpeech> PartOfSpeeches { get; } = new List<PartOfSpeech>();
    }
}
