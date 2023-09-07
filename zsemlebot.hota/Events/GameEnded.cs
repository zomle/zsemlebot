using zsemlebot.core.Domain;

namespace zsemlebot.hota.Events
{
    public class GameEnded : HotaEvent
    {
        public GameKey GameKey { get; }

        public GameEnded(GameKey gameKey)
        {
            GameKey = gameKey;
        }
    }
}
