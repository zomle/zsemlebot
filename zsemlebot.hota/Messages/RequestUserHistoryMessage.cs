using zsemlebot.hota.Extensions;

namespace zsemlebot.hota.Messages
{
	public class RequestUserHistoryMessage : HotaMessageBase
	{
		public uint TargetUserId { get; }

		public RequestUserHistoryMessage(uint targetUserId)
			: base(MessageType.RequestUserHistory, 0xc)
		{
			TargetUserId = targetUserId;
		}

		public override DataPackage AsDataPackage()
		{
			var buffer = CreateMessageBuffer();
			buffer.WriteInt(4, TargetUserId);
			buffer.WriteInt(8, 0xFFFFFFFF);
			return new DataPackage(buffer);
		}
	}
}
