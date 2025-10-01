//using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LearningWordsOnline.Models;
//using System.Data;
//using System.Drawing;
//using System.Reflection.Emit;

namespace LearningWordsOnline.Data
{
    public class LearningWordsOnlineDbContext : DbContext
    {
        public LearningWordsOnlineDbContext(DbContextOptions<LearningWordsOnlineDbContext> options)
            : base(options)
        {
        }

        public required DbSet<AppUser> AppUsers { get; set; }
        public required DbSet<AppUserDefinition> AppUserDefinitions { get; set; }
        public required DbSet<Battle> Battles { get; set; }
        public required DbSet<BattleAppUser> BattleAppUsers { get; set; }
        public required DbSet<Category> Categories { get; set; }
        public required DbSet<Definition> Definitions { get; set; }
        public required DbSet<DefinitionCategory> DefinitionCategories { get; set; }
        public required DbSet<Friend> Friends { get; set; }
        public required DbSet<FriendRequest> FriendRequests { get; set; }
        public required DbSet<Icon> Icons { get; set; }
        public required DbSet<Language> Languages { get; set; }
        public required DbSet<PartOfSpeech> PartOfSpeeches { get; set; }
        public required DbSet<Profile> Profiles { get; set; }
        public required DbSet<Pronunciation> Pronunciations { get; set; }
        public required DbSet<UserActivity> UserActivities { get; set; }
        public required DbSet<Word> Words { get; set; }
        public required DbSet<RoomInvitation> RoomInvitations { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>()
                .HasIndex(a => a.UserName)
                .IsUnique();

            modelBuilder.Entity<AppUserDefinition>()
                .HasKey(ad => new { ad.AppUserId, ad.DefinitionId }); // 複合主キーを設定
            modelBuilder.Entity<DefinitionCategory>()
                .HasKey(dc => new { dc.DefinitionId, dc.CategoryId }); // 複合主キーを設定
            modelBuilder.Entity<BattleAppUser>()
                .HasKey(ba => new { ba.BattleId, ba.AppUserId }); // 複合主キーを設定

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Sender)
                .WithMany(a => a.SentRequests)
                .HasForeignKey(fr => fr.AppUserId1)
                .OnDelete(DeleteBehavior.NoAction); // Sender (AppUserId1) が削除されたら、そのリクエストは削除しない

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany(a => a.ReceivedRequests)
                .HasForeignKey(fr => fr.AppUserId2)
                .OnDelete(DeleteBehavior.NoAction); // NOTE:Receiver (AppUserId2) が削除されても何もおきない循環参照削除エラーのため、つまり受信者が消されたらその人にかんするFriendRequestを何かしらで消す必要がある。

            modelBuilder.Entity<RoomInvitation>()
                .HasOne(ri => ri.Inviter)
                .WithMany(a => a.SentInvitations)
                .HasForeignKey(ri => ri.AppUserId1)
                .OnDelete(DeleteBehavior.NoAction); //NOTE: AppUser1が消されても招待状を削除しない

            modelBuilder.Entity<RoomInvitation>()
                .HasOne(ri => ri.Invitee)
                .WithMany(a => a.ReceivedInvitations)
                .HasForeignKey(ri => ri.AppUserId2)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Friend>()
                .HasOne(f => f.AppUser1)
                .WithMany(a => a.Friends1)
                .HasForeignKey(f => f.AppUserId1)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Friend>()
                .HasOne(f => f.AppUser2)
                .WithMany(a => a.Friends2)
                .HasForeignKey(f => f.AppUserId2)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.ChildCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict); // 親カテゴリ削除時に子カテゴリがいる場合は禁止する

            modelBuilder.Entity<Definition>()
                .HasOne(d => d.Word)
                .WithMany(w => w.Definitions)
                .HasForeignKey(d => d.WordId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Definition>()
                .HasOne(d => d.PartOfSpeech)
                .WithMany(pos => pos.Definitions)
                .HasForeignKey(d => d.PartOfSpeechId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
