using System;

namespace zsemlebot.hota.Events
{
    public class IncomingMessage : HotaEvent
    {
        public int SourceUserId { get; }
        public string Message { get; }

        public IncomingMessage(int sourceUserId, string message)
        {
            SourceUserId = sourceUserId;
            Message = message;
        }
    }
}
