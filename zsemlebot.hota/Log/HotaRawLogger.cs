using System;
using System.Text;
using zsemlebot.core.Log;

namespace zsemlebot.hota.Log
{
    public class HotaRawLogger : BinaryLogger
    {
        public static readonly HotaRawLogger Null = new HotaRawLogger();

        public HotaRawLogger(DateTime timestamp)
            : base($"hota_raw_{timestamp:yyyyMMdd_HHmmss}.bin")
        {
        }

        private HotaRawLogger()
            : base()
        {
        }
    }
}
