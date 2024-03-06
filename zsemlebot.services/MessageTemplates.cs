using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using zsemlebot.core.Domain;

namespace zsemlebot.services
{
	internal static class MessageTemplates
	{
		public static string CurrentGames(IReadOnlyList<string> descriptions)
		{
			if (descriptions.Count == 1)
			{
				return $"Current game: {descriptions[0]}";
			}
			else
			{
				return $"Current games: {string.Join("; ", descriptions)}";
			}
		}

		public static string GameNotFound(string user)
		{
			return $"{user} is not in a game at the moment.";
		}

		public static string UserLinkInvalidMessage()
		{
			return $"!link was incorrectly formatted. Usage: !link <add|del> {Constants.TwitchParameterPrefix}<twitch name> {Constants.HotaParameterPrefix}<hota name>";
		}

		public static string UserNotFound(string user)
		{
			return $"Can't find user '{user}'";
		}

		public static string TwitchUserNotFound(string user)
		{
			return $"Can't find twitch user '{user}'";
		}

		public static string HotaUserNotFound(string user)
		{
			return $"Can't find hota user '{user}'";
		}

		public static string UserLinkAlreadyLinked(string twitchUser, string hotaUser)
		{
			return $"Users are already linked. Twitch: '{twitchUser}', Hota: '{hotaUser}'";
		}

		public static string UserLinkTwitchMessage(string twitchUser, string hotaUser)
		{
			return $"Twitch user '{twitchUser}' is now linked with hota user '{hotaUser}'.";
		}

		public static string UserLinkTwitchMessage(string authCode, string twitchUser, string adminChannelName)
		{
			return $"Auth code: {authCode}. Send '!linkme {authCode}' as '{twitchUser}' on twitch in '{adminChannelName}' channel chat.";
		}

		public static string UserLinkDeleted(string twitchUser, string hotaUser)
		{
			return $"Twitch user '{twitchUser}' and hota user '{hotaUser}' link deleted.";
		}

		public static string UserLinkDoesntExist(string twitchUser, string hotaUser)
		{
			return $"Twitch user '{twitchUser}' and hota user '{hotaUser}' are not linked.";
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

		public static string InvalidOperation(string message)
		{
			return $"Invalid operation: {message}";
		}

		public static string GameDescription(QueriedHotaGame game)
		{
			var mainUserDesc = HotaUserDescription(game.UserOfInterest);

			var sb = new StringBuilder();
			foreach (var user in game.Game.JoinedUsers)
			{
				if (user.HotaUserId == game.UserOfInterest.HotaUserId)
				{
					continue;
				}

				if (sb.Length > 0)
				{
					sb.Append(", ");
				}

				sb.Append(HotaUserDescription(user));
			}

			if (sb.Length > 0)
			{
				return $"{mainUserDesc} vs {sb}";
			}
			else
			{
				return $"{mainUserDesc} without opp.";
			}
		}

		private static string HotaUserDescription(HotaUser user)
		{
			return $"{user.DisplayName}({user.Elo} elo)";
		}
	}
}
