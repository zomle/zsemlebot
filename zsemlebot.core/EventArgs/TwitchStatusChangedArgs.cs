using zsemlebot.core.Enums;

namespace zsemlebot.core.EventArgs
{
    public class TwitchStatusChangedArgs
    {
        public TwitchStatus NewStatus { get; }

        public TwitchStatusChangedArgs(TwitchStatus newStatus)
        {
            NewStatus = newStatus;
        }
    }
}
