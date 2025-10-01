using LearningWordsOnline.Models;

namespace LearningWordsOnline.ViewModels
{
    public class RankViewModel
    {
        public required AppUser AppUser { get; init; }
        public required IEnumerable<Battle> Battles { get; init; }
    }
}
