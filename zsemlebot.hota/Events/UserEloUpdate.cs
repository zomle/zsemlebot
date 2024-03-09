
namespace zsemlebot.hota.Events
{
	public class UserEloUpdate : HotaEvent
	{
		public uint HotaUserId { get; }
		public int Elo { get; }

		public UserEloUpdate(uint userId, int elo)
		{
			HotaUserId = userId;
			Elo = elo;           
		}
	}
}
