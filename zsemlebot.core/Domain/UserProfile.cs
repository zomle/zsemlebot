using System.Collections.Generic;

namespace zsemlebot.core.Domain
{
    public class UserProfile
    {
        public List<TwitchUser> TwitchUsers { get; }
        public List<HotaUser> HotaUsers { get; }

        public UserProfile()
        {
            TwitchUsers = new List<TwitchUser>();
            HotaUsers = new List<HotaUser>();
        }
    }
}
