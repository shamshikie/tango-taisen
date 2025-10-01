using LearningWordsOnline.Models;
using System.Collections.Concurrent;

namespace LearningWordsOnline.GameLogic
{
    public class Room : IRoom
    {
        public Room(string id, string hostId, Language language, MatchSettings settings)
            => (Id, HostId, Language, Settings) = (id, hostId, language, settings);

        public string Id { get; init; }
        public IList<Player> Players { get; } = new List<Player>();
        public string HostId { get; set; } // ホストプレイヤーのID
        public bool IsInProgress { get; set; } = false;
        public Language Language { get; set; }
        public int QuestionSentCount { get; set; } = 0;
        public IList<Question> Questions { get; } = new List<Question>();
        public ConcurrentQueue<Answer> Answers { get; } = new ConcurrentQueue<Answer>();
        public MatchSettings Settings { get; init; }
    }
}
