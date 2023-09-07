using System;
using zsemlebot.core.Enums;

namespace zsemlebot.core.EventArgs
{
    public class HotaStatusChangedArgs
    {
        public HotaClientStatus NewStatus { get; }
        public int? MinimumClientVersion { get; } 

        public HotaStatusChangedArgs(HotaClientStatus newStatus, int? minimumClientVersion = null)
        {
            NewStatus = newStatus;
            MinimumClientVersion = minimumClientVersion;
        }
    }
}
