using System;
using System.Text;
using zsemlebot.core.Log;

namespace zsemlebot.services.Log
{
    public class BotLogger : TextLogger
    {
        public static readonly BotLogger Instance;

        static BotLogger()
        {
            Instance = new BotLogger();
        }
        private BotLogger()
            : base($"bot_events_{DateTime.Now:yyyyMMdd_HHmmss}.txt", Encoding.UTF8)
        {
        }

        public void LogEvent(BotLogSource source, string message)
        {
            WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{source,-6}] - {message}");
        }
    }
}
