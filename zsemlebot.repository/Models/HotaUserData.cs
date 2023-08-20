using System;
using zsemlebot.core.Domain;

namespace zsemlebot.repository.Models
{
    internal class HotaUserData
    {
        public int HotaUserId { get; set; }
        public string DisplayName { get; set; }
        public int Elo { get; set; }
        public int Rep { get; set; }

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
        }
    }
}
