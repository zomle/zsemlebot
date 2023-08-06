using System;

namespace zsemlebot.hota.Messages
{
    public abstract class HotaMessageBase
    {
        public MessageType Type { get; set; }
        public int Length { get; set; }
        public virtual byte[] AsByteArray()
        {
            throw new NotImplementedException();
        }
    }
}
