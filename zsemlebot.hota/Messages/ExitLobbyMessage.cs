using zsemlebot.hota.Extensions;

namespace zsemlebot.hota.Messages
{
	public class ExitLobbyMessage : HotaMessageBase
	{
		public uint UserId { get; }

		public ExitLobbyMessage(uint userId)
			: base(MessageType.ExitLobby, 8)
		{
			UserId = userId;
		}

		public override DataPackage AsDataPackage()
		{
			var buffer = CreateMessageBuffer();
			buffer.WriteInt(4, UserId);
			return new DataPackage(buffer);
		}
	}
}
