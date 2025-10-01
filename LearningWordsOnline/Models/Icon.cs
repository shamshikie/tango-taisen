using System.ComponentModel.DataAnnotations;

namespace LearningWordsOnline.Models
{
    public class Icon
    {
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        [Required]
        public required string Url { get; set; }

        public virtual ICollection<Profile> Profiles { get; } = new List<Profile>();
    }
}
