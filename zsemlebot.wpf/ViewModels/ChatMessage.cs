using System;

namespace zsemlebot.wpf.ViewModels
{
    public class ChatMessage
    {
        public DateTime Timestamp { get; }
        public string Target { get; }
        public string Sender { get; }
        public string Message { get; }

        public ChatMessage(DateTime timestamp, string target, string sender, string message)
        {
            Timestamp = timestamp;
            Target = target;
            Sender = sender;
            Message = message;
        }
    }
}
