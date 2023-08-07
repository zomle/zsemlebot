using System;
using zsemlebot.core.Enums;

namespace zsemlebot.core.EventArgs
{
    public class HotaStatusChangedArgs
    {
        public HotaStatus NewStatus { get; }

        public HotaStatusChangedArgs(HotaStatus newStatus)
        {
            NewStatus = newStatus;
        }
    }
}
