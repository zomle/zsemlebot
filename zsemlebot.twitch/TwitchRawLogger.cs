using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zsemlebot.core;

namespace zsemlebot.twitch
{
    public class TwitchRawLogger : Logger
    {
        public TwitchRawLogger()
            : this(DateTime.Now)
        {
        }

        public TwitchRawLogger(DateTime timestamp)
            : base($"twitch_raw_{timestamp:yyyyMMdd_HHmmss}.txt")
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
