using LearningWordsOnline.Models;
namespace LearningWordsOnline.ViewModels
{
    public class FriendViewModel
    {
        public required int Id { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required AppUser FriendUser { get; init; }
        public required bool IsActive { get; init; }
    }
}
