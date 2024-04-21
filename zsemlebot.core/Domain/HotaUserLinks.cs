using System.Collections.Generic;

namespace zsemlebot.core.Domain
{
	public class HotaUserLinks
	{
		public HotaUser HotaUser { get; set; }
		public IReadOnlyList<TwitchUser> LinkedTwitchUsers { get; }

		public HotaUserLinks(HotaUser hotaUser, IReadOnlyList<TwitchUser> linkedTwitchUsers)
		{
			HotaUser = hotaUser;
			LinkedTwitchUsers = linkedTwitchUsers;
		}
	}
}
