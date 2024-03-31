using System.Collections.Generic;

namespace zsemlebot.services
{
	public class MapNameResponse
	{
		public Dictionary<uint, string> MapNames { get; }

		public MapNameResponse()
		{
			MapNames = new Dictionary<uint, string>();
		}
	}
}