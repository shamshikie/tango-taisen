using System.ComponentModel.DataAnnotations;

namespace LearningWordsOnline.Models
{
    public class Definition
    {
        public int Id { get; set; }

        [Required]
        public int WordId { get; set; }
        public Word Word { get; set; } = null!;

        [Required]
        public required int PartOfSpeechId { get; set; }
        public PartOfSpeech PartOfSpeech { get; set; } = null!;

        [Required]
        public required string Meaning { get; set; }

        public virtual ICollection<AppUserDefinition> AppUserDefinitions { get; } = new List<AppUserDefinition>();
        public virtual ICollection<DefinitionCategory> DefinitionCategories { get; } = new List<DefinitionCategory>();
    }
}
