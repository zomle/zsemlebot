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

		public static string MultipleGamesFound(string user)
		{
			return $"There seems to be multiple active games for {user}.";
		}

		public static string UserLinkInvalidMessage()
		{
			return $"!link was incorrectly formatted. Usage: !link <add|del> {Constants.TwitchParameterPrefix}<twitch name> {Constants.HotaParameterPrefix}<hota name>";
		}

		public static string ChannelInvalidMessage()
		{
			return $"!channel was incorrectly formatted. Usage: !channel <add|del> <#channelname>";
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

		public static string GameDescription(QueriedHotaGame gameInfo)
		{
			var mainUserDesc = HotaUserDescription(gameInfo.Game, gameInfo.UserOfInterest);

			var sb = new StringBuilder();
			foreach (var player in gameInfo.Game.JoinedPlayers)
			{
				if (player.HotaUserId == gameInfo.UserOfInterest.HotaUserId)
				{
					continue;
				}

				if (sb.Length > 0)
				{
					sb.Append(", ");
				}

				sb.Append(HotaUserDescription(gameInfo.Game, player.HotaUser));
			}

			string template = "";
			if (!string.IsNullOrWhiteSpace(gameInfo.Game.Template))
			{
				template = $"{gameInfo.Game.Template}; ";
			}

			if (sb.Length > 0)
			{
				return $"{template}{mainUserDesc} vs {sb}";
			}
			else
			{
				return $"{template}{mainUserDesc} without opp.";
			}
		}

		private static string HotaUserDescription(HotaGame game, HotaUser user)
		{
			var sb = new StringBuilder();
			sb.Append($"{user.Elo} elo");

			var playerInfo = game.GetPlayer(user);
			if (playerInfo != null)
			{
				if (!string.IsNullOrWhiteSpace(playerInfo.Color))
				{
					sb.Append($", {playerInfo.Color}");
				}

				if (!string.IsNullOrWhiteSpace(playerInfo.Faction))
				{
					sb.Append($", {playerInfo.Faction}");
				}

				if (playerInfo.Trade != null)
				{
					sb.Append($", ");
					if (playerInfo.Trade > 0)
					{
						sb.Append('+');
					}
					else if (playerInfo.Trade < 0)
					{
						sb.Append('-');
					}

					sb.Append(playerInfo.Trade);
					sb.Append(" gold");
				}
			}

			return $"{user.DisplayName}({sb})";
		}

		public static string OppDescriptions(List<HotaUser> users)
		{
			if (users.Count == 0)
			{
				return "No opponent yet.";
			}

			var sb = new StringBuilder();
			foreach (var opp in users)
			{
				if (sb.Length > 0)
				{
					sb.Append(", ");
				}
				sb.Append($"{opp.DisplayName}({opp.Elo} elo, {opp.Rep} rep)");
			}

			return $"Current opponent{(users.Count == 1 ? "" : "s")}: {sb}";
		}

		public static string JoiningChannel(string targetChannel)
		{
			return $"Joining '{targetChannel}' channel.";
		}

		public static string LeavingChannel(string targetChannel)
		{
			return $"Leaving '{targetChannel}' channel.";
		}

		public static string HistoryTodaySingleAccount(string queriedUser, int wins, int losses, int eloChange, int currentElo)
		{ 
			string endS = queriedUser.EndsWith('s') ? "" : "s";
			string winPl = wins == 1 ? "" : "s";
			string losePl = losses == 1 ? "" : "es";
			string eloDelta = $"{(eloChange > 0 ? "+" : "-")}{Math.Abs(eloChange)}";

			return $"{queriedUser}'{endS} stats for today are: {wins} win{winPl}, {losses} loss{losePl}. ({eloDelta} elo, {currentElo} total)";
		}

		public static string HistoryTodayMultipleAccount(string queriedUser, int wins, int losses, int eloChange, int accountCount)
		{
			string endS = queriedUser.EndsWith('s') ? "" : "s";
			string winPl = wins == 1 ? "" : "s";
			string losePl = losses == 1 ? "" : "es";
			string eloDelta = $"{(eloChange > 0 ? "+" : "-")}{Math.Abs(eloChange)}";

			return $"{queriedUser}'{endS} stats for today are: {wins} win{winPl}, {losses} loss{losePl}. ({eloDelta} elo, on {accountCount} accounts)";
		}

		public static string HistoryTodayNoInfo(string queriedUser)
		{
			return $"Couldn't find any ranked games finished today for {queriedUser}.";
		}
	}
}
