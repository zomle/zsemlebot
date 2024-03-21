using System;
using System.Collections.Generic;
using System.Linq;
using zsemlebot.core;
using zsemlebot.core.Domain;
using zsemlebot.repository.Models;

namespace zsemlebot.repository
{
	public class BotRepository : ZsemlebotRepositoryBase
	{
		public static readonly BotRepository Instance;

		private Dictionary<uint, List<TwitchHotaLink>> LinksByHotaUserId { get; set; }
		private Dictionary<int, List<TwitchHotaLink>> LinksByTwitchUserId { get; set; }
		private List<TwitchHotaLinkRequestData> UserLinkRequests { get; set; }
		private List<JoinedChannel> JoinedChannels { get; set; }
		private List<ZsemlebotSetting> ZsemlebotSettings { get; set; }

		static BotRepository()
		{
			Instance = new BotRepository();
		}

		private BotRepository()
		{
			LinksByHotaUserId = new Dictionary<uint, List<TwitchHotaLink>>();
			LinksByTwitchUserId = new Dictionary<int, List<TwitchHotaLink>>();

			UserLinkRequests = new List<TwitchHotaLinkRequestData>();
			JoinedChannels = new List<JoinedChannel>();
			ZsemlebotSettings = new List<ZsemlebotSetting>();

			LoadTwitchHotaUserLinks();
			LoadTwitchHotaUserLinkRequests();
			LoadJoinedChannels();
			LoadBotSettings();
		}

		public IReadOnlyList<IReadOnlyZsemlebotSetting> ListZsemlebotSettings(Predicate<ZsemlebotSetting> predicate)
		{
			return ZsemlebotSettings
					.Where(s => predicate(s))
					.ToList();
		}

		public IReadOnlyList<TwitchUserLinks> ListUserLinks()
		{
			var result = new List<TwitchUserLinks>();

			foreach (var linkKv in LinksByTwitchUserId)
			{
				var twitchUser = TwitchRepository.Instance.GetUser(linkKv.Key);
				if (twitchUser == null)
				{
					continue;
				}

				var hotaUsers = new List<HotaUser>();
				foreach (var links in linkKv.Value)
				{
					var hotaUser = HotaRepository.Instance.GetUser(links.HotaUserId);
					if (hotaUser == null)
					{
						continue;
					}

					hotaUsers.Add(hotaUser);
				}

				result.Add(new TwitchUserLinks(twitchUser, hotaUsers));
			}

			return result;
		}

		public void AddJoinedChannel(int twitchUserId)
		{
			if (JoinedChannels.Any(jc => jc.TwitchUserId == twitchUserId))
			{
				return;
			}

			JoinedChannels.Add(new JoinedChannel { TwitchUserId = twitchUserId });
			EnqueueWorkItem(@$"INSERT INTO [{JoinedChannelsTableName}] ([TwitchUserId]) 
                           VALUES (@twitchUserId);", new { twitchUserId });
		}

		public void DeleteJoinedChannel(int twitchUserId)
		{
			JoinedChannels.RemoveAll(jc => jc.TwitchUserId == twitchUserId);
			EnqueueWorkItem(@$"DELETE FROM [{JoinedChannelsTableName}] 
							WHERE [TwitchUserId] = @twitchUserId;", new { twitchUserId });
		}

		public bool HasJoinedChannel(int twitchUserId)
		{
			return JoinedChannels.Any(jc => jc.TwitchUserId == twitchUserId);
		}

		public IEnumerable<TwitchUserData> ListJoinedChannels()
		{
			var userData = Query<TwitchUserData>($"SELECT tu.[TwitchUserId], tu.[DisplayName] " +
												$"FROM [{JoinedChannelsTableName}] jc " +
												$"JOIN [{TwitchUserDataTableName}] tu on jc.[TwitchUserId] = tu.[TwitchUserId];");
			return userData;
		}

		public void UpdateZsemlebotSetting(int? targetTwitchUserId, int? channelTwitchUserId, string settingName, string settingValue)
		{
			var setting = ZsemlebotSettings.FirstOrDefault(s => s.TargetTwitchUserId == targetTwitchUserId
													&& s.ChannelTwitchUserId == channelTwitchUserId
													&& string.Equals(s.SettingName, settingName, StringComparison.CurrentCultureIgnoreCase));

			if (setting == null)
			{
				setting = new ZsemlebotSetting
				{
					TargetTwitchUserId = targetTwitchUserId,
					ChannelTwitchUserId = channelTwitchUserId,
					SettingName = settingName,
					SettingValue = settingValue
				};
				ZsemlebotSettings.Add(setting);

				var param = new
				{
					targetTwitchUserId = setting.TargetTwitchUserId,
					channelTwitchUserId = setting.ChannelTwitchUserId,
					settingName = setting.SettingName,
					settingValue = setting.SettingValue
				};
				EnqueueWorkItem(@$"INSERT INTO [{ZsemlebotSettingsTableName}] ([TargetTwitchUserId], [ChannelTwitchUserId], [SettingName], [SettingValue]) 
                           VALUES (@targetTwitchUserId, @channelTwitchUserId, @settingName, @settingValue);", param);
			}
			else
			{
				setting.SettingValue = settingValue;

				var param = new
				{
					targetTwitchUserId = setting.TargetTwitchUserId,
					channelTwitchUserId = setting.ChannelTwitchUserId,
					settingName = setting.SettingName,
					settingValue = setting.SettingValue
				};
				EnqueueWorkItem(@$"UPDATE [{ZsemlebotSettingsTableName}] 
									SET [SettingValue] = @settingValue 
									WHERE [TargetTwitchUserId] {(targetTwitchUserId == null ? "IS NULL" : "= @targetTwitchUserId")}
										AND [ChannelTwitchUserId] {(channelTwitchUserId == null ? "IS NULL" : "= @channelTwitchUserId")}
										AND [SettingName] = @settingName;", param);
			}
		}

		public void DeleteZsemlebotSetting(int? targetTwitchUserId, int? channelTwitchUserId, string settingName)
		{
			int removed = ZsemlebotSettings.RemoveAll(s => s.TargetTwitchUserId == targetTwitchUserId
													&& s.ChannelTwitchUserId == channelTwitchUserId
													&& string.Equals(s.SettingName, settingName, StringComparison.CurrentCultureIgnoreCase));

			if (removed > 0)
			{
				EnqueueWorkItem(@$"DELETE FROM [{ZsemlebotSettingsTableName}] 
									WHERE [TargetTwitchUserId] {(targetTwitchUserId == null ? "IS NULL" : "= @targetTwitchUserId")}
										AND [ChannelTwitchUserId] {(channelTwitchUserId == null ? "IS NULL" : "= @channelTwitchUserId")}
										AND [SettingName] = @settingName;", new { targetTwitchUserId, channelTwitchUserId, settingName });
			}
		}

		public void CreateUserLinkRequest(uint hotaUserId, string twitchUserName, string authCode, int validityLengthInMins)
		{
			//delete potentially existing request
			DeleteUserLinkRequest(hotaUserId, twitchUserName);

			var request = new TwitchHotaLinkRequestData { TwitchUserName = twitchUserName, HotaUserId = hotaUserId, AuthCode = authCode, ValidUntilUtc = DateTime.UtcNow + TimeSpan.FromMinutes(validityLengthInMins) };
			UserLinkRequests.Add(request);

			EnqueueWorkItem(@$"INSERT INTO [{TwitchHotaUserLinkRequestTableName}] ([TwitchUserName], [HotaUserId], [AuthCode], [ValidUntilUtc]) 
                           VALUES (@twitchUserName, @hotaUserId, @authCode, datetime('now', '+{validityLengthInMins} minutes'));", new { twitchUserName, hotaUserId, authCode });
		}

		public TwitchHotaLinkRequest? GetUserLinkRequest(uint hotaUserId, string twitchUserName)
		{
			var result = UserLinkRequests
				.Where(r => r.HotaUserId == hotaUserId && string.Equals(r.TwitchUserName, twitchUserName, StringComparison.InvariantCultureIgnoreCase))
				.Select(r => new TwitchHotaLinkRequest { HotaUserId = r.HotaUserId, TwitchUserName = r.TwitchUserName, AuthCode = r.AuthCode, ValidUntilUtc = r.ValidUntilUtc })
				.FirstOrDefault();
			return result;
		}

		public IReadOnlyList<TwitchHotaLinkRequest> ListUserLinkRequests(string twitchUserName)
		{
			var result = UserLinkRequests
				.Where(r => string.Equals(r.TwitchUserName, twitchUserName, StringComparison.InvariantCultureIgnoreCase))
				.Select(r => new TwitchHotaLinkRequest { HotaUserId = r.HotaUserId, TwitchUserName = r.TwitchUserName, AuthCode = r.AuthCode, ValidUntilUtc = r.ValidUntilUtc })
				.ToList();
			return result;
		}

		public void DeleteUserLinkRequest(uint hotaUserId, string twitchUserName)
		{
			UserLinkRequests.RemoveAll(r => r.HotaUserId == hotaUserId && string.Equals(r.TwitchUserName, twitchUserName, StringComparison.InvariantCultureIgnoreCase));
			EnqueueWorkItem(@$"DELETE FROM [{TwitchHotaUserLinkRequestTableName}]
                           WHERE [TwitchUserName] = @twitchUserName AND [HotaUserId] = @hotaUserId", new { twitchUserName, hotaUserId });
		}

		public void UpdateUserLinkRequest(TwitchHotaLinkRequest request, int validityLengthInMins)
		{
			request.ValidUntilUtc = DateTime.UtcNow + TimeSpan.FromMinutes(validityLengthInMins);
			EnqueueWorkItem(@$"UPDATE [{TwitchHotaUserLinkRequestTableName}]
                                SET [ValidUntilUtc] = datetime('now', '+{validityLengthInMins} minutes')
                                WHERE [TwitchUserName] = @twitchUserName AND [HotaUserId] = @hotaUserId",
								new { twitchUserName = request.TwitchUserName, hotaUserId = request.HotaUserId });
		}

		public void AddTwitchHotaUserLink(int twitchUserId, uint hotaUserId)
		{
			var newLink = new TwitchHotaLink { TwitchUserId = twitchUserId, HotaUserId = hotaUserId };
			AddTwitchHotaUserLink(newLink);

			EnqueueWorkItem(@$"INSERT INTO [{TwitchHotaUserLinkTableName}] ([TwitchUserId], [HotaUserId], [CreatedAtUtc]) 
                           VALUES (@twitchUserId, @hotaUserId, datetime('now'));", new { twitchUserId, hotaUserId });
		}

		public void DelTwitchHotaUserLink(int twitchUserId, uint hotaUserId)
		{
			if (LinksByHotaUserId.TryGetValue(hotaUserId, out var lst))
			{
				lst.RemoveAll(l => l.TwitchUserId == twitchUserId && l.HotaUserId == hotaUserId);
			}

			if (LinksByTwitchUserId.TryGetValue(twitchUserId, out lst))
			{
				lst.RemoveAll(l => l.TwitchUserId == twitchUserId && l.HotaUserId == hotaUserId);
			}

			EnqueueWorkItem(@$"DELETE FROM [{TwitchHotaUserLinkTableName}]
                                WHERE [TwitchUserId] = @twitchUserId AND [HotaUserId] = @hotaUserId;", new { twitchUserId, hotaUserId });
		}

		public TwitchUserLinks? GetLinksForTwitchName(string twitchUserName)
		{
			var twitchUser = TwitchRepository.Instance.GetUser(twitchUserName);
			if (twitchUser == null)
			{
				return null;
			}

			return GetLinksForTwitchUser(twitchUser);
		}

		public TwitchUserLinks? GetLinksForTwitchId(int twitchUserId)
		{
			var twitchUser = TwitchRepository.Instance.GetUser(twitchUserId);
			if (twitchUser == null)
			{
				return null;
			}

			return GetLinksForTwitchUser(twitchUser);
		}

		public TwitchUserLinks GetLinksForTwitchUser(TwitchUser twitchUser)
		{
			if (!LinksByTwitchUserId.TryGetValue(twitchUser.TwitchUserId, out var links))
			{
				return new TwitchUserLinks(twitchUser, Array.Empty<HotaUser>());
			}

			var linkedHotaUsers = new List<HotaUser>();
			foreach (var link in links)
			{
				var hotaUser = HotaRepository.Instance.GetUser(link.HotaUserId);
				if (hotaUser == null)
				{
					continue;
				}

				linkedHotaUsers.Add(hotaUser);
			}

			return new TwitchUserLinks(twitchUser, linkedHotaUsers);
		}

		private void LoadTwitchHotaUserLinkRequests()
		{
			var models = Query<TwitchHotaLinkRequestData>($"SELECT [TwitchUserName], [HotaUserId], [AuthCode], [ValidUntilUtc] FROM [{TwitchHotaUserLinkRequestTableName}];");
			foreach (var model in models)
			{
				UserLinkRequests.Add(model);
			}
		}

		private void LoadJoinedChannels()
		{
			var models = Query<JoinedChannel>($"SELECT [TwitchUserId] FROM [{JoinedChannelsTableName}];");
			foreach (var model in models)
			{
				JoinedChannels.Add(model);
			}
		}

		private void LoadBotSettings()
		{
			var models = Query<ZsemlebotSetting>($"SELECT [TargetTwitchUserId], [ChannelTwitchUserId], [SettingName], [SettingValue] FROM [{ZsemlebotSettingsTableName}];");
			foreach (var model in models)
			{
				ZsemlebotSettings.Add(model);
			}
		}

		private void LoadTwitchHotaUserLinks()
		{
			var models = Query<TwitchHotaLink>($"SELECT [TwitchUserId], [HotaUserId] FROM [{TwitchHotaUserLinkTableName}];");
			foreach (var model in models)
			{
				AddTwitchHotaUserLink(model);
			}
		}

		private void AddTwitchHotaUserLink(TwitchHotaLink link)
		{
			if (!LinksByHotaUserId.TryGetValue(link.HotaUserId, out var lst))
			{
				lst = new List<TwitchHotaLink>();
				LinksByHotaUserId.Add(link.HotaUserId, lst);
			}
			lst.Add(link);

			if (!LinksByTwitchUserId.TryGetValue(link.TwitchUserId, out lst))
			{
				lst = new List<TwitchHotaLink>();
				LinksByTwitchUserId.Add(link.TwitchUserId, lst);
			}
			lst.Add(link);
		}
	}
}
