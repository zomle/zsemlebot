using System.Collections.Generic;
using zsemlebot.repository.Models;

namespace zsemlebot.repository
{
	public class OldBotRepository: RepositoryBase
	{
		public OldBotRepository(string dbfilePath)
			: base(dbfilePath)
		{
		}

		public IEnumerable<OldTwitchUserData> ListTwitchUsers()
		{
			var users = Query<OldTwitchUserData>($"SELECT [UserId], [DisplayName] FROM [TwitchUserData];");
			return users;
		}

		public IEnumerable<OldHotaUserData> ListHotaUsers()
		{
			var users = Query<OldHotaUserData>($"SELECT [UserId], [UserName], [UserElo], [LastUpdatedAtUtc] FROM [HotaUserData];");
			return users;
		}
	}
}
