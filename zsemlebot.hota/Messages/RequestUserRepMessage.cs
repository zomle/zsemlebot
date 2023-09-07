using zsemlebot.hota.Extensions;

namespace zsemlebot.hota.Messages
{
    public class RequestUserRepMessage : HotaMessageBase
    {
        public uint TargetUserId { get; }

        public RequestUserRepMessage(uint targetUserId)
            : base(MessageType.RequestUserRep, 8)
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
