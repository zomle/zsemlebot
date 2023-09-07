using System;
using System.Collections.Generic;

namespace zsemlebot.core.Domain
{
    public class TwitchUserLinks
    {
        public TwitchUser TwitchUser { get; }
        public IReadOnlyList<HotaUser> LinkedHotaUsers { get; }

        public TwitchUserLinks(TwitchUser twitchUser, IReadOnlyList<HotaUser> linkedHotaUsers)
        {
            TwitchUser = twitchUser;
            LinkedHotaUsers = linkedHotaUsers;
        }
    }
}
