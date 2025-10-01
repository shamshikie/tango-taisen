namespace LearningWordsOnline.GameLogic
{
    public class MatchSettings
    {
        public required int MaxPlayerCount { get; init; }
        public required int QuestionCount { get; init; }
        public required int OptionCount { get; init; }
        public required int Timer { get; init; }
        public required IReadOnlyList<int> Points { get; init; }
        //public required IReadOnlyList<int> RankPoints;
    }
}
