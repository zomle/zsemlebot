using System;

namespace zsemlebot.wpf.ViewModels
{
    public class ChatMessage
    {
        public DateTime Timestamp { get; }
        public string Channel { get; }
        public string Sender { get; }
        public string Message { get; }

        public ChatMessage(DateTime timestamp, string channel, string sender, string message)
        {
            Timestamp = timestamp;
            Channel = channel;
            Sender = sender;
            Message = message;
        }
    }
}
