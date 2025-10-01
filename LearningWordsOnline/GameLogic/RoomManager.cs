using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Text;
using LearningWordsOnline.Models;

namespace LearningWordsOnline.GameLogic
{
    public static class RoomManager //Static?
    {
        //stringにroomId
        private static ConcurrentDictionary<string, Room> rooms = new();

        public static Room CreateRoom(string hostId, MatchSettings settings, Language language)
        {
            //グローバル一意識別子を生成する
            string roomId;
            // 重複していないRoomIdを取得
            do
            {
                roomId = Guid.NewGuid().ToString();
            } while (GetRoom(roomId) is not null);

            Room room = new Room(roomId, hostId, language, settings);
            rooms[roomId] = room;
            return room;
        }

        /// <summary>
        /// If a room which matchs the roomId is not found, this returns null
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public static Room? GetRoom(string roomId)
        {
            rooms.TryGetValue(roomId, out var room);
            return room;
        }

        public static Room? GetRoomByPlayerId(string playerId)
        {
            // 各部屋を検索して、プレイヤーが含まれている部屋を返す
            return rooms.Values.FirstOrDefault(room => room.Players.Any(player => player.Id == playerId));
        }

        public static void RemoveRoom(string roomId)
        {
            rooms.TryRemove(roomId, out _);
        }

        //public static void AddPlayerToRoom(string roomId, string player)
        //{
        //	if (rooms.TryGetValue(roomId, out var room) && room.Players.Count < 4)
        //	{
        //		room.Players.Add(player);
        //	}
        //}
    }

    public static class RankRoomManager //Static?
    {
        //stringにroomId
        private static ConcurrentDictionary<string, RankRoom> rooms = new();

        public static RankRoom CreateRoom(MatchSettings settings, IList<Question> questions, Language language)
        {
            string roomId;

            // 重複していないRoomIdを取得
            do
            {
                roomId = Guid.NewGuid().ToString();
            } while (GetRoom(roomId) is not null);

            //グローバル一意識別子）を生成する
            var room = new RankRoom { Id = roomId, Settings = settings, Language = language };
            foreach (var question in questions)
            {
                room.Questions.Add(question);
            }
            rooms[roomId] = room;
            return room;
        }

        /// <summary>
        /// If a room which matchs the roomId is not found, this returns null
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public static RankRoom? GetRoom(string roomId)
        {
            rooms.TryGetValue(roomId, out var room);
            return room;
        }

        public static RankRoom? GetRoomByPlayerId(string playerId)
        {
            // 各部屋を検索して、プレイヤーが含まれている部屋を返す
            return rooms.Values.FirstOrDefault(room => room.Players.Any(player => player.Id == playerId));
        }

        public static void RemoveRoom(string roomId)
        {
            rooms.TryRemove(roomId, out _);
        }

        //public static void AddPlayerToRoom(string roomId, string player)
        //{
        //	if (rooms.TryGetValue(roomId, out var room) && room.Players.Count < 4)
        //	{
        //		room.Players.Add(player);
        //	}
        //}
    }
}
