namespace zsemlebot.repository.Models
{
	public class ZsemlebotSetting : IReadOnlyZsemlebotSetting
	{
		public int? TargetTwitchUserId { get; set; }
		public int? ChannelTwitchUserId { get; set; }
		public string SettingName { get; set; }
		public string? SettingValue { get; set; }
	}
}
