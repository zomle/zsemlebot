using zsemlebot.core.Domain;

namespace zsemlebot.hota.Events
{
    public class GameRoomUserJoined : HotaEvent
    {
        public GameKey GameKey { get; }
        public uint OtherUserId { get; }

        public GameRoomUserJoined(GameKey gameKey, uint otherUserId)
        {
            GameKey = gameKey;
            OtherUserId = otherUserId;
        }
    }
}
