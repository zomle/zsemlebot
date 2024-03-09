using System.Collections.Generic;
using zsemlebot.core.Domain;
using zsemlebot.hota.Events;

namespace zsemlebot.services
{
	public class GameHistoryResponse
	{
		public List<HotaUser> UpdatedUsers { get; set; }
		public List<HotaUser> NotUpdatedUsers { get; set; }

		public GameHistoryResponse(IEnumerable<HotaUser> updatedUsers, IEnumerable<HotaUser> notUpdatedUsers)
		{
			UpdatedUsers = new List<HotaUser>(updatedUsers);
			NotUpdatedUsers = new List<HotaUser>(notUpdatedUsers);
		}
	}
}