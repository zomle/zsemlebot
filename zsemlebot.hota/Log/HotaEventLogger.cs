using System;
using System.Text;
using zsemlebot.core.Extensions;
using zsemlebot.core.Log;
using zsemlebot.hota.Extensions;

namespace zsemlebot.hota.Log
{
    public class HotaEventLogger : TextLogger
    {
        public static readonly HotaEventLogger Null = new HotaEventLogger();

        public HotaEventLogger(DateTime timestamp)
             : base($"hota_events_{timestamp:yyyyMMdd_HHmmss}.txt", Encoding.UTF8)
        {
        }

        private HotaEventLogger()
            : base()
        {
        }

        internal void LogEvent(short type, string summary, string? details = null)
        {
            WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | Type: {type.ToHexString()} ({summary,-14}); {details ?? string.Empty}");
        }
    }
}
