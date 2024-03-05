using System;
using System.Linq;
using zsemlebot.repository;

namespace zsemlebot.services
{
	public class BotService : IDisposable
	{
		public void MigrateOldData(string sourceDatabaseFilePath)
		{
			var oldRepository = new OldBotRepository(sourceDatabaseFilePath);


			var oldTwitchUsers = oldRepository.ListTwitchUsers();
			foreach (var user in oldTwitchUsers)
			{
				TwitchRepository.Instance.UpdateTwitchUserName(user.UserId, user.DisplayName);
			}

			var oldHotaUsers = oldRepository.ListHotaUsers();
			foreach (var user in oldHotaUsers)
			{
				var newUser = new core.Domain.HotaUser(user.UserId, user.UserName, user.UserElo, 0, null, user.LastUpdatedAtUtc);
				HotaRepository.Instance.UpdateHotaUser(newUser);
			}

			var oldJoinedChannels = oldRepository.ListJoinedChannels();
			foreach (var channel in oldJoinedChannels)
			{
				var userName = channel.Name[1..];
				var newUser = TwitchRepository.Instance.GetUser(userName);

				if (newUser != null)
				{
					BotRepository.Instance.AddJoinedChannel(newUser.TwitchUserId);
				}
				else
				{
					throw new Exception($"Can't find user: '{userName}'");
				}
			}

			var oldUserLinks = oldRepository.ListUserLinks();
			foreach (var link in oldUserLinks)
			{
				var twitchUser = TwitchRepository.Instance.GetUser(link.TwitchUserId);
				if (twitchUser == null)
				{
					continue;
				}

				var hotaUser = HotaRepository.Instance.GetUser(link.HotaUserId);
				if (hotaUser == null)
				{
					continue;
				}

				var links = BotRepository.Instance.GetLinksForTwitchId(link.TwitchUserId);
				if (links != null)
				{
					if (links.LinkedHotaUsers.Any(hu => hu.HotaUserId == link.HotaUserId))
					{
						continue;
					}
				}

				BotRepository.Instance.AddTwitchHotaUserLink(link.TwitchUserId, link.HotaUserId);
			}
		}

		#region IDisposable implementation
		private bool disposedValue;
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					//
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
