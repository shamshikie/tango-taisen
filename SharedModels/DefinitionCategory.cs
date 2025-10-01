using System.ComponentModel.DataAnnotations;

namespace LearningWordsOnline.Models
{
    public class DefinitionCategory
    {
        [Required]
        public int DefinitionId { get; set; }
        public required Definition Definition { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public required Category Category { get; set; }

    }
}
