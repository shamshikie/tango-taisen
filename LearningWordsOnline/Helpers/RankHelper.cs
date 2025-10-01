namespace LearningWordsOnline.Helpers
{
    public class RankHelper
    {
        public static string GetRank(int rankPoints)
        {
            switch (rankPoints)
            {
                case < 0:
                    return "?";
                case < 100:
                    return "D-";
                case < 200:
                    return "D";
                case < 300:
                    return "D+";
                case < 400:
                    return "C-";
                case < 500:
                    return "C";
                case < 600:
                    return "C+";
                case < 700:
                    return "B-";
                case < 800:
                    return "B";
                case < 900:
                    return "B+";
                case < 1000:
                    return "A-";
                case < 1100:
                    return "A";
                default:
                    return "A+";
            }
        }
    }
}
