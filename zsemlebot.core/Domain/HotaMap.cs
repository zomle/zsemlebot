namespace zsemlebot.core.Domain
{
	public class HotaMap
	{
		public uint HotaMapId { get; set; }
		public string DisplayName { get; set; }

		public HotaMap(uint hotaMapId, string displayName)
		{
			HotaMapId = hotaMapId;
			DisplayName = displayName;
		}
	}
}