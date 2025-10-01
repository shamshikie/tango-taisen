using System.ComponentModel.DataAnnotations;

namespace LearningWordsOnline.Models
{
    public class Definition
    {
        public int Id { get; set; }

        [Required]
        public int WordId { get; set; }
        public required Word Word { get; set; }

        [Required]
        public int PartOfSpeechId { get; set; }
        public required PartOfSpeech PartOfSpeech { get; set; }

        [Required]
        public required string Meaning { get; set; }

        public virtual ICollection<AppUserDefinition> AppUserDefinitions { get; } = new List<AppUserDefinition>();
        public virtual ICollection<DefinitionCategory> DefinitionCategories { get; } = new List<DefinitionCategory>();
    }
}
