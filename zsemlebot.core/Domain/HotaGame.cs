using System.Collections.Generic;
using System.Linq;
using zsemlebot.core.Enums;

namespace zsemlebot.core.Domain
{
	public class HotaGame
	{
		public virtual bool IsRealGame { get { return true; } }

		public HotaGameStatus Status { get; set; }

		public GameKey GameKey { get; set; }
		public HotaUser HostUser { get; set; }
		public string Description { get; set; }
		public bool IsLoaded { get; set; }
		public bool IsRanked { get; set; }
		public int MaxPlayerCount { get; set; }
		public List<HotaGamePlayer> JoinedPlayers { get; set; }
		public string Template { get; set; }

		public bool IsStarted { get { return Status == HotaGameStatus.InProgress; } }

		public HotaGame(GameKey gameKey, HotaUser hostUser, string description)
		{
			JoinedPlayers = new List<HotaGamePlayer>();

			GameKey = gameKey;
			HostUser = hostUser;
			Description = description;
		}

		public HotaGamePlayer? GetPlayer(HotaUser hotaUser)
		{
			return JoinedPlayers.FirstOrDefault(jp => jp.HotaUserId == hotaUser.HotaUserId);
		}
	}
}