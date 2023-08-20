using System;
using zsemlebot.core.Enums;

namespace zsemlebot.core.EventArgs
{
    public class HotaStatusChangedArgs
    {
        public HotaStatus NewStatus { get; }
        public int? MinimumClientVersion { get; } 

        public HotaStatusChangedArgs(HotaStatus newStatus, int? minimumClientVersion = null)
        {
            NewStatus = newStatus;
            MinimumClientVersion = minimumClientVersion;
        }
    }
}
