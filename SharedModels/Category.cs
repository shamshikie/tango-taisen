using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace LearningWordsOnline.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        public int LanguageId { get; set; }

        public required Language Language { get; set; }

        [Required]
        [MaxLength(16)]
        public required string Name { get; set; }

        public int? ParentCategoryId { get; set; } = null;
        public Category? ParentCategory { get; set; }

        public virtual ICollection<Category> ChildCategories { get; } = new List<Category>();
        public virtual ICollection<DefinitionCategory> DefinitionCategories { get; } = new List<DefinitionCategory>();
    }
}
