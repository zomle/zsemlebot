﻿using System;
using zsemlebot.hota.Extensions;

namespace zsemlebot.hota.Messages
{
    public abstract class HotaMessageBase
    {
        public MessageType Type { get; set; }
        public short Length { get; set; }
        
        public virtual byte[] AsByteArray()
        {
            throw new NotImplementedException();
        }

        protected byte[] CreateMessageBuffer()
        {
            if (Length == < 4)
            {
                throw new InvalidOperationException("Message length should be at least 4 (sizeof(Length) + sizeof(Type)).");
            }

            var result = new byte[Length];
            result.WriteShort(0, Length);
            result.WriteShort(2, (short)Type);
            return result;
        }
    }
}
