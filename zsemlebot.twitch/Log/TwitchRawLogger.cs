using System;
using zsemlebot.core.Log;

namespace zsemlebot.twitch.Log
{
    public class TwitchRawLogger : Logger
    {
        public static readonly TwitchRawLogger Null = new TwitchRawLogger();

        public TwitchRawLogger(DateTime timestamp)
            : base($"twitch_raw_{timestamp:yyyyMMdd_HHmmss}.txt")
        {
        }

        private TwitchRawLogger()
            : base()
        {
        }

        public void WriteIncomingMessage(string line)
        {
            WriteLine($"< {line}");
        }

        public void WriteOutgoingMessage(string line)
        {
            WriteLine($"> {line}");
        }
    }
}
