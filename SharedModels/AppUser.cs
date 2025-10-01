using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime;

namespace LearningWordsOnline.Models
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        [Column(TypeName = "NVARCHAR(450)")]
        public required string AspNetUserId { get; set; }


        [Required]
        [EmailAddress]
        [MaxLength(250)]
        [Column(TypeName = "NVARCHAR(256)")]
        public required string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(12, MinimumLength = 6, ErrorMessage = "{0} は {2} 文字以上 {1} 文字以内でなければなりません。")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "{0} は英数字とアンダースコア(_)のみ使用可能です。")]
        [Display(Name = "ユーザーID")]
        //NOTE: Fluent APIでユニークの条件付け
        public required string UserName { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; }

        [Required]
        public required DateTime UpdatedAt { get; set; }

        public virtual Profile Profile { get; set; } = default!;
        public virtual UserActivity? UserActivity { get; set; } = default;

        public virtual ICollection<BattleAppUser> BattleAppUsers { get; } = new List<BattleAppUser>();
        public virtual ICollection<AppUserDefinition> AppUserDefinitions { get; } = new List<AppUserDefinition>();


        // フレンドリクエストのナビゲーションプロパティ
        [InverseProperty("Sender")]
        public virtual ICollection<FriendRequest> SentRequests { get; } = new List<FriendRequest>(); // 自分が送ったリクエスト
        [InverseProperty("Receiver")]
        public virtual ICollection<FriendRequest> ReceivedRequests { get; } = new List<FriendRequest>();// 自分が受け取ったリクエスト

        // Friends1: UserId1として関連付けられたFriendエンティティ
        [InverseProperty("AppUser1")]
        public virtual ICollection<Friend> Friends1 { get; } = new List<Friend>();

        [InverseProperty("AppUser2")]
        // Friends2: UserId2として関連付けられたFriendエンティティ
        public virtual ICollection<Friend> Friends2 { get; } = new List<Friend>();

        // 部屋招待状のナビゲーションプロパティ
        [InverseProperty("Inviter")]
        public virtual ICollection<RoomInvitation> SentInvitations { get; } = new List<RoomInvitation>(); // 自分が招待
        [InverseProperty("Invitee")]
        public virtual ICollection<RoomInvitation> ReceivedInvitations { get; } = new List<RoomInvitation>();// 受け取った招待状

    }
}
