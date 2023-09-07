using System;
using System.Collections.Generic;
using System.Linq;
using zsemlebot.core.Domain;

namespace zsemlebot.services
{
    internal static class MessageTemplates
    {
        public static string UserNotFound(string user)
        {
            return $"Can't find user '{user}'";
        }

        public static string UserLinkTwitchMessage(string twitchUser, string hotaUser)
        {
            return $"Your twitch user '{twitchUser}' is now linked with your hota lobby user '{hotaUser}'.";
        }

        public static string UserLinkTwitchMessage(string authCode, string twitchUser, string adminChannelName)
        {
            return $"Auth code: {authCode}. Send '!linkme {authCode}' as '{twitchUser}' on twitch in '{adminChannelName}' channel chat.";
        }

        public static string EloForTwitchUser(TwitchUser twitchUser, IEnumerable<HotaUser> hotaUsers)
        {
            var userElos = hotaUsers
                .OrderBy(hu => hu.DisplayName)
                .Select(hu => $"{hu.DisplayName}({hu.Elo})");

            return $"{twitchUser.DisplayName} twitch user's elo in hota: {string.Join(", ", userElos)}";
        }

        public static string EloForHotaUser(IReadOnlyList<HotaUser> hotaUsers)
        {
            if (hotaUsers.Count == 1)
            {
                var hotaUser = hotaUsers[0];

                return $"{hotaUser.DisplayName} hota user's elo is {hotaUser.Elo}";
            }
            else
            {
                var userElos = hotaUsers
                    .OrderBy(hu => hu.DisplayName)
                    .Select(hu => $"{hu.DisplayName}({hu.Elo})");

                return $"Hota users' elos are {string.Join(", ", userElos)}";
            }
        }

        public static string RepForTwitchUser(TwitchUser twitchUser, IEnumerable<HotaUser> hotaUsers)
        {
            var userReps = hotaUsers
                .OrderBy(hu => hu.DisplayName)
                .Select(hu => $"{hu.DisplayName}({hu.Rep})");

            return $"{twitchUser.DisplayName} twitch user's rep in hota: {string.Join(", ", userReps)}";
        }

        public static string RepForHotaUser(IReadOnlyList<HotaUser> hotaUsers)
        {
            if (hotaUsers.Count == 1)
            {
                var hotaUser = hotaUsers[0];

                return $"{hotaUser.DisplayName} hota user's rep is {hotaUser.Rep}";
            }
            else
            {
                var userReps = hotaUsers
                    .OrderBy(hu => hu.DisplayName)
                    .Select(hu => $"{hu.DisplayName}({hu.Rep})");

                return $"Hota users' reps are {string.Join(", ", userReps)}";
            }
        }
    }
}
