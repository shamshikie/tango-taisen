using LearningWordsOnline.Models;
namespace LearningWordsOnline.ViewModels
{
    public class RoomViewModel
    {
        public required string AspNetUserId { get; init; }
        public required string RoomId { get; init; }
        //public required string RoomUrl { get; init; }
        public required int MaxPlayerCount { get; init; }
        public required bool IsInProgress { get; init; }

        public required IEnumerable<FriendViewModel> FriendViewModels { get; init; }
    }
}
