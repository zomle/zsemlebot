namespace zsemlebot.repository.Models
{
	public interface IReadOnlyZsemlebotSetting
	{
		int? TargetTwitchUserId { get; }
		int? ChannelTwitchUserId { get; }
		string SettingName { get; }
		string? SettingValue { get; }
	}
}
