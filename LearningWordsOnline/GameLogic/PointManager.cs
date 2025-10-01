using System.ComponentModel;
using System.Numerics;

namespace LearningWordsOnline.GameLogic
{
    public static class PointManager
    {
        public static void DistributePoints(IEnumerable<Player> players, IEnumerable<Answer> answers, IReadOnlyList<int> points)
        {
            // プレイヤーをIDで高速に検索できるようにDictionaryに変換
            var playerDictionary = players.ToDictionary(p => p.Id);

            ValidatePlayerIds(answers, playerDictionary);

            // 回答時間の昇順でソート（早い順）
            var sortedAnswers = answers.OrderByDescending(a => a.RemainingTime);
            int currentRank = 0;
            int lastRemainingTime = -1;
            int playersAtRank = 0;
            foreach (var answer in sortedAnswers)
            {
                if (answer.RemainingTime != lastRemainingTime)
                {
                    // 順位更新
                    currentRank += playersAtRank;
                    playersAtRank = 0;
                }

                if (currentRank < points.Count)
                {
                    int point = points[currentRank];
                    if (playerDictionary.TryGetValue(answer.AspNetUserId, out var player))
                    {
                        // 正誤に応じてポイントを更新、最小0に設定
                        switch (answer.AnswerStatus)
                        {
                            case Evaluation.Incorrect:
                                player.Points -= point;
                                break;
                            case Evaluation.Correct:
                                player.Points += point;
                                break;
                            case Evaluation.NoAnswer:
                                break;
                            default:
                                throw new InvalidEnumArgumentException(nameof(answer.AnswerStatus), (int)answer.AnswerStatus, typeof(Evaluation));
                        }
                        player.Points = Math.Max(0, player.Points);
                    }
                }

                playersAtRank++;
                lastRemainingTime = answer.RemainingTime;
            }
        }


        public static void DeterminePositions(IEnumerable<Player> players)
        {
            var sortedPlayers = players.OrderByDescending(p => p.Points).ToList();

            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                sortedPlayers[i].Position = i + 1;
                // 同じポイントのプレイヤーには同じ順位を割り当てる
                if (i > 0 && sortedPlayers[i].Points == sortedPlayers[i - 1].Points)
                {
                    sortedPlayers[i].Position = sortedPlayers[i - 1].Position;
                }
            }
        }

        //public static void CalculateRankPoint(IEnumerable<Player> players, IReadOnlyList<int> rankPoints)
        //{
        //    foreach (var player in players)
        //    {
        //        player.RankPoints += rankPoints;
        //    }
        //}
        /// <summary>
        /// すべてのanswersのUserIdがplayersに存在するか確認
        /// </summary>
        /// <param name="answers"></param>
        /// <param name="playerDictionary"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private static void ValidatePlayerIds(IEnumerable<Answer> answers, Dictionary<string, Player> playerDictionary)
        {
            foreach (var answer in answers)
            {
                if (!playerDictionary.ContainsKey(answer.AspNetUserId))
                {
                    throw new InvalidOperationException($"Player with ID {answer.AspNetUserId} not found.");
                }
            }
        }
    }
}
