using System;

namespace zsemlebot.repository.Models
{
    internal class HotaUserData
    {
        public int HotaUserId { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string DisplayName { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public int Elo { get; set; }
        public int Rep { get; set; }
    }
}
