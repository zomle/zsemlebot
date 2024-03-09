using zsemlebot.core.Domain;

namespace zsemlebot.hota.Events
{
	public class GameRoomUserLeft : HotaEvent
	{
		public GameKey GameKey { get; }
		public uint OtherUserId { get; }

		public GameRoomUserLeft(GameKey gameKey, uint otherUserId)
		{
			GameKey = gameKey;
			OtherUserId = otherUserId;
		}
	}
}
