namespace zsemlebot.hota.Events
{
	public class MapInfo : HotaEvent
	{
		public uint MapId { get; }
		public string MapName { get; }

		public MapInfo(uint mapId, string mapName)
		{
			MapId = mapId;
			MapName = mapName;
		}
	}
}
