using LearningWordsOnline.GameLogic;
using LearningWordsOnline.Models;
using System.Collections.Concurrent;

namespace LearningWordsOnline.GameLogic
{
    public interface IRoom
    {
        string Id { get; init; }
        IList<Player> Players { get; }
        Language Language { get; }
        int QuestionSentCount { get; set; }
        IList<Question> Questions { get; }
        ConcurrentQueue<Answer> Answers { get; }
        MatchSettings Settings { get; init; }
    }
}