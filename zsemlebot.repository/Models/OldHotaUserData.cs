using System;

namespace zsemlebot.repository.Models
{
	public class OldHotaUserData
	{
		public uint UserId { get; set; }
		public string UserName { get; set; }
		public int UserElo { get; set; }
		public DateTime LastUpdatedAtUtc { get; set; }
	}
}
