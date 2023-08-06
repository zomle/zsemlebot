using System;

namespace zsemlebot.wpf.ViewModels
{
    public class ChatMessage
    {
        public DateTime Timestamp { get; set; }
        public string Channel { get; set; }
        public string Sender { get; set; }
        public string Message { get; set; }
    }
}
