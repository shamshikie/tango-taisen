using LearningWordsOnline.Models;
using System.Collections.Concurrent;
namespace LearningWordsOnline.GameLogic
{
    public class RankRoom : IRoom
    {
        public required string Id { get; init; }
        public IList<Player> Players { get; } = new List<Player>();
        public required Language Language { get; init; }
        public int QuestionSentCount { get; set; } = 0;
        public IList<Question> Questions { get; } = new List<Question>();
        public ConcurrentQueue<Answer> Answers { get; } = new ConcurrentQueue<Answer>();
        public required MatchSettings Settings { get; init; }
    }
}
