using System;
using zsemlebot.core.Enums;

namespace zsemlebot.core.Domain
{
	public class HotaUser
    {
        public uint HotaUserId { get; set; }
        public HotaUserStatus? Status { get; set; }
        public string DisplayName { get; set; }
        public int Elo { get; set; }
        public int Rep { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public HotaUser(uint hotaUserId, string displayName, int elo, int rep, HotaUserStatus? status, DateTime updatedAtUtc)
        {
            HotaUserId = hotaUserId;
            DisplayName = displayName;
            Elo = elo;
            Rep = rep;
            Status = status;
            UpdatedAtUtc = updatedAtUtc;
        }
    }
}