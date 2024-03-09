using System.Collections.Generic;

namespace zsemlebot.hota.Events
{
	public class GameHistory : HotaEvent
	{
		public int AllGames { get; set; }
		public uint MainHotaUserId { get; set; }
		public List<GameHistoryEntry> Entries { get; set; }

		public GameHistory()
		{
			Entries = new List<GameHistoryEntry>();
		}
	}
}
