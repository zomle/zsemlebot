namespace zsemlebot.hota.Messages
{
    public class MaybePingMessage : HotaMessageBase
    {
        public MaybePingMessage() 
            : base(MessageType.MaybePing, 4)
        {
        }

        public override DataPackage AsDataPackage()
        {
            return new DataPackage(CreateMessageBuffer());
        }
    }
}
