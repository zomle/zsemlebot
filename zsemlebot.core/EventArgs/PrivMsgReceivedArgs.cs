using System;

namespace zsemlebot.core.EventArgs
{
    public class PrivMsgReceivedArgs
    {
        public DateTime Timestamp { get; }
        public string Channel { get;  }
        public string Sender { get; }
        public string Message { get; }

        public PrivMsgReceivedArgs(string channel, string sender, string message)
        {
            Timestamp = DateTime.Now;
            Channel = channel;
            Sender = sender;
            Message = message;
        }
    }
}
