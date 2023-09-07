using System;
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

        public HotaUserData()
        {
            DisplayName = string.Empty;
        }

        public HotaUserData(HotaUser user)
        {
            HotaUserId = user.HotaUserId;
            DisplayName = user.DisplayName;
            Elo = user.Elo;
            Rep = user.Rep;
            LastUpdatedAtUtc = user.UpdatedAtUtc;
        }
    }
}
