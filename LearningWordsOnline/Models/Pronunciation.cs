using System.ComponentModel.DataAnnotations;

namespace LearningWordsOnline.Models
{
    public class Pronunciation
    {
        public int Id { get; set; }

        [Required]
        public int WordId { get; set; }
        public required Word Word { get; set; }

        [Required]
        public required string Symbol { get; set; }

        // オーディオデータをbyte配列で格納
        public byte[]? Voice { get; set; } = null;

    }
}
