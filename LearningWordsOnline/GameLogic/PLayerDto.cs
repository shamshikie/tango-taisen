namespace LearningWordsOnline.GameLogic
{
    public class PlayerDto
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string? IconUrl { get; init; }
        public required int Points { get; init; }
        public static IEnumerable<PlayerDto> GetPlayersForClient(IEnumerable<Player> players)
            => players.Select(p => ConvertToDto(p));

        private static PlayerDto ConvertToDto(Player player)
            => new PlayerDto
            {
                Id = player.Id,
                Name = player.Name,
                Points = player.Points,
                IconUrl = player.IconUrl
            };

    }
}
