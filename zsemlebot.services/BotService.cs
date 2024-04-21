using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using zsemlebot.core.Domain;
using zsemlebot.repository;

namespace zsemlebot.services
{
	public class BotService : IDisposable
	{
		private TwitchRepository TwitchRepository { get { return TwitchRepository.Instance; } }
		private BotRepository BotRepository { get { return BotRepository.Instance; } }

		private static readonly HttpClient PublicIpHttpClient;

		static BotService()
		{
			PublicIpHttpClient = new HttpClient() { BaseAddress = new Uri("https://api.ipify.org/") };
		}

		public IReadOnlyList<ZsemlebotSetting> ListSettings()
		{
			var settings = BotRepository.ListZsemlebotSettings(p => true);

			var result = new List<ZsemlebotSetting>();
			foreach (var setting in settings)
			{
				var user = setting.TargetTwitchUserId == null ? null : TwitchRepository.GetUser(setting.TargetTwitchUserId.Value);
				var channel = setting.ChannelTwitchUserId == null ? null : TwitchRepository.GetUser(setting.ChannelTwitchUserId.Value);

				result.Add(new ZsemlebotSetting(user, channel, setting.SettingName, setting.SettingValue));
			}

			return result;
		}

		public IReadOnlyList<TwitchUser> ListJoinedChannels()
		{
			var twitchUsers = BotRepository.ListJoinedChannels();

			var result = new List<TwitchUser>();
			foreach (var twitchUser in twitchUsers)
			{
				var tmp = new TwitchUser(twitchUser.TwitchUserId, twitchUser.DisplayName);
				result.Add(tmp);
			}
			return result;
		}

		public IReadOnlyList<TwitchUserLinks> ListLinkedUsers()
		{
			var userLinks = BotRepository.ListUserLinks();
			return userLinks;
		}

		public async Task<string> GetPublicIp()
		{
			var response = await PublicIpHttpClient.GetStringAsync((string?)null);
			return response;
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
