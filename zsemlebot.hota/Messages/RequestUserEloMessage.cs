using System;
using zsemlebot.hota.Extensions;

namespace zsemlebot.hota.Messages
{
    public class RequestUserEloMessage : HotaMessageBase
    {
        public uint TargetUserId { get; }

        public RequestUserEloMessage(uint targetUserId)
            : base(MessageType.RequestUserElo, 8)
        {
            TargetUserId = targetUserId;
        }

        public override DataPackage AsDataPackage()
        {
            var buffer = CreateMessageBuffer();
            buffer.WriteInt(4, TargetUserId);
            return new DataPackage(buffer);
        }
    }
}
