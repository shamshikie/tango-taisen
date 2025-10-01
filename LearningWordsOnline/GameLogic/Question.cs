namespace LearningWordsOnline.GameLogic
{
    public class Question
    {
        public required string Text { get; init; }
        public required IEnumerable<string> Options { get; init; }
        public required string CorrectAnswer { get; init; }
        public bool IsCorrectAnswerSent { get; set; } = false;
        public required int DefinitionId { get; init; }
    }
}
