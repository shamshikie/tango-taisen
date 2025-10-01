namespace LearningWordsOnline.GameLogic
{
    public class Answer
    {
        public required string AspNetUserId { get; init; }
        public required string SelectedAnswer { get; init; }
        public required int RemainingTime { get; init; }
        public Evaluation AnswerStatus { get; private set; }
        public override string ToString() => $"{AspNetUserId}\n{SelectedAnswer}:{RemainingTime}\n";

        public void CheckAnswer(string CorrectAnswer)
        {
            if (SelectedAnswer == string.Empty)
                AnswerStatus = Evaluation.NoAnswer;
            else
                AnswerStatus = SelectedAnswer == CorrectAnswer ?
                    Evaluation.Correct : Evaluation.Incorrect;
        }
    }
}
