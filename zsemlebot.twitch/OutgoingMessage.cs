using System;

namespace zsemlebot.twitch
{
    public class OutgoingMessage
    {
        public DateTime CreatedAt { get; }
        public string Message { get; }
        public string? LogMessageOverride { get; }

        public OutgoingMessage(string message, string? logMessageOverride)
        {
            CreatedAt = DateTime.Now;

            Message = message;
            LogMessageOverride = logMessageOverride;
        }
    }
}
