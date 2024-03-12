using zsemlebot.core.Domain;

namespace zsemlebot.services
{
	public class ZsemlebotSetting
	{
		public TwitchUser? TwitchUser { get; }
		public TwitchUser? TwitchChannel { get;  }
		public string SettingName { get; }
		public string? SettingValue { get; }

		public ZsemlebotSetting(TwitchUser? twitchUser, TwitchUser? twitchChannel, string settingName, string? settingValue)
		{
			TwitchUser = twitchUser;
			TwitchChannel = twitchChannel;
			SettingName = settingName;
			SettingValue = settingValue;
		}
	}
}
