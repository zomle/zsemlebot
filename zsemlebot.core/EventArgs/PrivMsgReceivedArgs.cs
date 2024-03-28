using System;

namespace zsemlebot.core.EventArgs
{
    public class PrivMsgReceivedArgs
    {
        public DateTime Timestamp { get; }
        public string Target { get;  }
        public string Sender { get; }
        public string Message { get; }

        public PrivMsgReceivedArgs(string target, string sender, string message)
        {
            Timestamp = DateTime.Now;
            Target = target;
            Sender = sender;
            Message = message;
        }
    }
}
