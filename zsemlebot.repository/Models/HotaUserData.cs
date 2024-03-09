using System;
using System.Collections.Generic;
using zsemlebot.core.Domain;

namespace zsemlebot.repository.Models
{
    internal class HotaUserData
    {
        public uint HotaUserId { get; set; }
        public string DisplayName { get; set; }
        public int Elo { get; set; }
        public int Rep { get; set; }
        public DateTime LastUpdatedAtUtc { get; set; }
		public Dictionary<uint, HotaUserGameHistoryEntry> GameHistory { get;  set; }
		public bool GameHistoryUpToDate { get; internal set; }

		public HotaUserData()
        {
            DisplayName = string.Empty;
			GameHistory = new Dictionary<uint, HotaUserGameHistoryEntry>();
		}

        public HotaUserData(HotaUser user)
        {
            HotaUserId = user.HotaUserId;
            DisplayName = user.DisplayName;
            Elo = user.Elo;
            Rep = user.Rep;
            LastUpdatedAtUtc = user.UpdatedAtUtc;
			GameHistory = new Dictionary<uint, HotaUserGameHistoryEntry>();
		}
    }
}
