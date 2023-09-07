
using System.Collections.Generic;
using zsemlebot.core.Domain;

namespace zsemlebot.hota.Events
{
    public class GameRoomCreated : HotaEvent
    {
        public GameKey GameKey { get; }
        public string Description { get; }
        public bool IsRanked { get; }
        public bool IsLoadGame { get; }
        public int MaxPlayerCount { get; }
        public IReadOnlyList<uint> PlayerIds { get; }

        public GameRoomCreated(GameKey gameKey, string description, bool isRanked, bool isLoadGame, int maxPlayerCount, IEnumerable<uint> playerIds)
        {
            GameKey = gameKey;
            Description = description;
            IsRanked = isRanked;
            IsLoadGame = isLoadGame;
            MaxPlayerCount = maxPlayerCount;
            PlayerIds = new List<uint>(playerIds);
        }
    }
}
