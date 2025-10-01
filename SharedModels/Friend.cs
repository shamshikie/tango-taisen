using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningWordsOnline.Models
{
    public class Friend
    {
        public int Id { get; set; }

        // NOTE: UserId1 < UserId2 になるように設計
        [Required]
        public required int AppUserId1 { get; set; }

        [ForeignKey("AppUserId1")]
        [InverseProperty("Friends1")]  // AppUser1の友達関係
        public AppUser AppUser1 { get; set; } = null!;

        [Required]
        public required int AppUserId2 { get; set; }

        [ForeignKey("AppUserId2")]
        [InverseProperty("Friends2")]  // AppUser1の友達関係
        public AppUser AppUser2 { get; set; } = null!;

        [Required]
        public required DateTime CreatedAt { get; set; }

        public static Friend Create(AppUser appUser1, AppUser appUser2)
        {
            if (appUser1.Id > appUser2.Id)
            {
                return new Friend() { AppUserId1 = appUser2.Id, AppUserId2 = appUser1.Id, CreatedAt = DateTime.UtcNow };
            }
            else if (appUser2.Id > appUser1.Id)
            {
                return new Friend() { AppUserId1 = appUser1.Id, AppUserId2 = appUser2.Id, CreatedAt = DateTime.UtcNow };
            }
            else
                throw new ArgumentException("引数が同じ値です。");
        }
    }
}
