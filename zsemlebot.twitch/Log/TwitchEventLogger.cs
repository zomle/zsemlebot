using System;
using zsemlebot.core.Log;

namespace zsemlebot.twitch.Log
{
    public class TwitchEventLogger : Logger
    {
        public static readonly TwitchEventLogger Null = new TwitchEventLogger();

        public TwitchEventLogger(DateTime timestamp)
             : base($"twitch_events_{timestamp:yyyyMMdd_HHmmss}.txt")
        {
        }

        private TwitchEventLogger()
            : base()
        {
        }

        public void LogJoinChannel(string channel)
        {
            WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} > JOIN {channel}");
        }

        public void LogPartChannel(string channel)
        {
            WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} > PART {channel}");
        }

        public void LogPrivMsg(Message message)
        {
            var sender = message.Source == null ? "(null)" : message.Source.Split('!')[0];

            var paramTokens = message.Params.Split(' ', 2);
            var channel = paramTokens[0];
            var messageText = paramTokens[1][1..];
            WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} < {channel} - {sender} :{messageText}");
        }

        public void LogSentMsg(string channel, string message)
        {
            WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} > {channel} :{message}");
        }

        public void Connected()
        {
            WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} < Connected to Twitch.");
        }
        public void LogPing()
        {
            WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} < PING");
        }

        public void LogPong()
        {
            WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} < PONG");
        }
    }
}
