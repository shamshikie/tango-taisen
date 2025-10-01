using LearningWordsOnline.Models;

namespace LearningWordsOnline.ViewModels
{
    public class EditProfileViewModel
    {
        public required AppUser AppUser { get; init; }
        public required IEnumerable<Icon> Icons { get; init; }
    }
}
