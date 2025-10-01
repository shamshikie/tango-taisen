using LearningWordsOnline.Models;

namespace LearningWordsOnline.ViewModels
{
    public class MyWordsViewModel
    {
        public int WordId { get; init; }
        public int? Level { get; init; }
        public required string Spelling { get; init; }
        public required string LanguageCode { get; init; }
        public required IList<DefinitionViewModel> DefinitionViewModels { get; init; }
        public int? CorrectAnswerRate { get; init; }
    }

    public class DefinitionViewModel
    {
        public required int DefinitionId { get; init; }
        public required string Meaning { get; init; }
        public required string PartOfSpeech { get; init; }
        public AppUserDefinition? AppUserDefinition { get; init; }
    }
}
