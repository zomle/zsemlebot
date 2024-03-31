using zsemlebot.hota.Extensions;

namespace zsemlebot.hota.Messages
{
	public class RequestGameHistoryMessage : HotaMessageBase
	{
		public uint TargetUserId { get; }

		public RequestGameHistoryMessage(uint targetUserId)
			: base(MessageType.RequestGameHistory, 0xc)
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
