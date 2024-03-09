
namespace zsemlebot.hota.Events
{
	public class UserRepUpdate : HotaEvent
	{
		public uint HotaUserId { get; }
		public short FriendLists { get; }
		public short BlackLists { get; }

		public UserRepUpdate(uint userId, short friendLists, short blackLists)
		{
			HotaUserId = userId;
			FriendLists = friendLists;
			BlackLists = blackLists;
		}
	}
}
