using zsemlebot.core.Enums;

namespace zsemlebot.core.EventArgs
{
    public class TwitchStatusChangedArgs
    {
        public object? Client { get; }
        public TwitchStatus NewStatus { get; }

        public TwitchStatusChangedArgs(TwitchStatus newStatus, object? client)
        {
            NewStatus = newStatus;
            Client = client;
        }
    }
}
