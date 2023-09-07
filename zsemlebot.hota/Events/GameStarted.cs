using zsemlebot.core.Domain;

namespace zsemlebot.hota.Events
{
    public class GameStarted : HotaEvent
    {
        public GameKey GameKey { get; }

        public GameStarted(GameKey gameKey)
        {
            GameKey = gameKey;
        }
    }
}
