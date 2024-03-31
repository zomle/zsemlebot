using zsemlebot.core.Enums;

namespace zsemlebot.core.Domain
{
	public class HotaUserGameHistoryPlayer
	{
		public uint UserId { get; init; }
		public HotaTown Town { get; init; }
		public HotaColor Color { get; init; }
		public byte Hero { get; init; }
		public int OldElo { get; init; }
		public int NewElo { get; init; }
		public int EloChange { get { return NewElo - OldElo; } }
	}
}
