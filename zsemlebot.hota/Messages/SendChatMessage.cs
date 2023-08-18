using System;

namespace zsemlebot.hota.Messages
{
    public partial class SendChatMessage : HotaMessageBase
    {
        private const short MessageBoilerPlateLength = 0x33;

        public string Text { get; set; }
        public uint TargetUserId { get; set; }

        public SendChatMessage(uint targetUserId, string text) 
            : base(MessageType.SendChatMessage, (short)(text.Length + MessageBoilerPlateLength + 1))
        {
            TargetUserId = targetUserId;
            Text = text;
        }

        //The implementation of `DataPackage AsDataPackage()` is kept in a separate file, that is not commited to git.
        //The reason for this is to not to make it easy for potential bad actors to abuse the hota lobby.
    }
}
