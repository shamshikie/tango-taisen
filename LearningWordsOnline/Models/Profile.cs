using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningWordsOnline.Models
{
    public class Profile
    {
        public int Id { get; set; }

        [Required]
        public int AppUserId { get; set; }

        public virtual AppUser? AppUser { get; set; } = null!;

        private string displayName = string.Empty;

        [Required(ErrorMessage = "{0}は必須です。")]
        [StringLength(10, MinimumLength = 1, ErrorMessage = "{0} は {2} 文字以上 {1} 文字以内でなければなりません。")]
        [Display(Name = "ニックネーム")]
        public string DisplayName
        {
            get => displayName;
            set => displayName = value?.Trim() ?? string.Empty; // ここで前後のスペースを削除
        }

        private string? bio;

        [StringLength(160, MinimumLength = 0, ErrorMessage = "{0} は {2} 文字以上 {1} 文字以内でなければなりません。")]
        public string? Bio
        {
            get => bio;
            set => bio = value?.Trim(); // ここで前後のスペースを削除
        }

        public int? IconId { get; set; }
        public virtual Icon? Icon { get; set; }

        private int rankPoint = 0;

        [Required]
        public int RankPoint
        {
            get => rankPoint;
            set => rankPoint = Math.Max(0, value);
        }


        [Required]
        // 他のユーザーが閲覧可能な状態
        public bool IsPublic { get; set; } = true;

        [Required]
        public DateTime UpdatedAt { get; set; }
    }
}
