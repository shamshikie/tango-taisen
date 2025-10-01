namespace LearningWordsOnline.GameLogic
{
    public class Player
    {
        public required string Id { get; init; } //本アプリではAspNetUserIdが入る
        public required string ConnectionId { get; set; }
        public required string? IconUrl { get; init; }
        public int Points { get; set; } = 0;
        public int Position { get; set; } = 0;
        public int RankPoints { get; set; } = 0;
        public string Name { get; init; } = string.Empty;
        public bool IsInGame { get; set; } = false;
        public override string ToString()
        {
            return $"{Id}:{Points}ポイント\n";
        }
    }
}
