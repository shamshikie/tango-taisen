using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningWordsOnline.Models
{
    public class Profile
    {
        public int Id { get; set; }

        [Required]
        public required int AppUserId { get; set; }

        public virtual AppUser? AppUser { get; set; } = null!;

        [Required]
        [StringLength(10, MinimumLength = 1)]
        [Display(Name = "ニックネーム")]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(160, MinimumLength = 0, ErrorMessage = "Bio cannot be longer than 160 characters.")]
        public string? Bio { get; set; } = string.Empty;

        public int? IconId { get; set; }
        public virtual Icon? Icon { get; set; }

        [Required]
        public int RankPoint { get; set; } = 0;

        [Required]
        // 他のユーザーが閲覧可能な状態
        public bool IsPublic { get; set; } = true;

        [Required]
        public DateTime UpdatedAt { get; set; }
    }
}
