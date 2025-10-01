using System.ComponentModel.DataAnnotations;

namespace LearningWordsOnline.Models
{
    public class DefinitionCategory
    {
        [Required]
        public int DefinitionId { get; set; }
        public Definition Definition { get; set; } = null!;

        [Required]
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

    }
}
