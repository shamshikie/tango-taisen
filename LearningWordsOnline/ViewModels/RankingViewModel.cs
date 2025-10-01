using LearningWordsOnline.Models;
namespace LearningWordsOnline.ViewModels
{
    public class RankingViewModel
    {
        public required IEnumerable<UserPosition> Leaderboard { get; init; }
        public UserPosition? MyOwnPosition { get; init; }
    }

    public class UserPosition
    {
        public int Position { get; init; }
        public required AppUser AppUser { get; init; }
    }
}
