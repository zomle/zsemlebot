using System.Collections.Generic;
using System.Data.SQLite;
using zsemlebot.core;
using zsemlebot.repository.Models;

namespace zsemlebot.repository
{
	public class OldBotRepository : RepositoryBase
	{
		public string DbFilePath { get; }

		public OldBotRepository(string dbfilePath)
		{
			DbFilePath = dbfilePath;
		}

		protected override SQLiteConnection GetConnection()
		{
			var connectionString = GetConnectionString(DbFilePath);

			var connection = new SQLiteConnection(connectionString);
			connection.Open();

			return connection;
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

		public IEnumerable<OldJoinedChannel> ListJoinedChannels()
		{
			var channels = Query<OldJoinedChannel>($"SELECT [Name] FROM [JoinedChannels];");
			return channels;
		}

		public IEnumerable<OldUserLink> ListUserLinks()
		{
			var links = Query<OldUserLink>($"SELECT [TwitchUserId], [HotaUserId] FROM [TwitchUserHotaUserLink];");
			return links;
		}
	}
}
