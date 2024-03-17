using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using zsemlebot.core.Domain;
using zsemlebot.core.Enums;
using zsemlebot.repository.Models;

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
			return $"{Constants.Command_Link} was incorrectly formatted. Usage: {Constants.Command_Link} <add|del> {Constants.TwitchParameterPrefix}<twitch name> {Constants.HotaParameterPrefix}<hota name>";
		}

		public static string ChannelInvalidMessage()
		{
			return $"{Constants.Command_Channel} was incorrectly formatted. Usage: {Constants.Command_Channel} <add|del> <#channelname>";
		}

		public static string ZsemlebotInvalidMessage()
		{
			return $"{Constants.Command_Zsemlebot} was incorrectly formatted. Check documentation for details.";
		}

		public static string ZsemlebotInvalidOptionMessage(string providedOption, string[] allOptions)
		{
			return $"{Constants.Command_Zsemlebot} was provided an invalid option: {providedOption}. Valid options: {string.Join(", ", allOptions)}";
		}

		public static string ZsemlebotInvalidCommandMessage(string providedCommand, string[] allCommands)
		{
			return $"{Constants.Command_Zsemlebot} was provided an invalid command: {providedCommand}. Valid commands: {string.Join(", ", allCommands)}";
		}

		public static string IgnoreInvalidMessage()
		{
			return $"{Constants.Command_Ignore} was incorrectly formatted. Usage: {Constants.Command_Ignore} <add|del> <twitch user> or {Constants.Command_Ignore} list";
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
			return $"Auth code: {authCode}. Send '{Constants.Command_LinkMe} {authCode}' as '{twitchUser}' on twitch in '{adminChannelName}' channel chat.";
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

					sb.Append(Math.Abs(playerInfo.Trade ?? 0));
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

		public static string HistoryTodaySingleAccount(string queriedUser, int wins, int losses, int eloChange, int currentElo, TimeSpan timezoneOffset)
		{ 
			string endS = queriedUser.EndsWith('s') ? "" : "s";
			string winPl = wins == 1 ? "" : "s";
			string losePl = losses == 1 ? "" : "es";
			string eloDelta = $"{(eloChange < 0 ? "-" : "+")}{Math.Abs(eloChange)}";

			return $"{queriedUser}'{endS} stats for today are: {wins} win{winPl}, {losses} loss{losePl}. ({eloDelta} elo, {currentElo} total; considering {GetTimeZoneFromOffset(timezoneOffset)} timezone)";
		}

		public static string HistoryTodayMultipleAccount(string queriedUser, int wins, int losses, int eloChange, int accountCount, TimeSpan timezoneOffset)
		{
			string endS = queriedUser.EndsWith('s') ? "" : "s";
			string winPl = wins == 1 ? "" : "s";
			string losePl = losses == 1 ? "" : "es";
			string eloDelta = $"{(eloChange < 0 ? "-" : "+")}{Math.Abs(eloChange)}";

			return $"{queriedUser}'{endS} stats for today are: {wins} win{winPl}, {losses} loss{losePl}. ({eloDelta} elo, on {accountCount} accounts; considering {GetTimeZoneFromOffset(timezoneOffset)} timezone)";
		}

		public static string HistoryTodayNoInfo(string queriedUser, TimeSpan timezoneOffset)
		{
			//return $"Couldn't find any ranked games finished today for {queriedUser} (considering {GetTimeZoneFromOffset(timezoneOffset)} timezone).";
			string endS = queriedUser.EndsWith('s') ? "" : "s";
			return $"{queriedUser}'{endS} stats for today are: 0 wins, 0 losses. (considering {GetTimeZoneFromOffset(timezoneOffset)} timezone)";
		}

		private static string GetTimeZoneFromOffset(TimeSpan offset)
		{
			var sb = new StringBuilder("UTC");
			if (offset.Hours == 0 && offset.Minutes == 0)
			{
				return sb.ToString();
			}

			sb.Append(offset.TotalHours < 0 ? '-' : '+');
			sb.Append(Math.Abs(offset.Hours));
			if (offset.Minutes != 0) 
			{
				sb.Append(':');
				sb.Append(offset.Minutes.ToString("00"));
			}
			return sb.ToString();
		}

		public static string Streak(string queriedUser, bool winStreak, int streak)
		{
			var winlose = winStreak ? "winning" : "losing";
			return $"{queriedUser} is on a {winlose} streak, {winlose} {streak} games in a row.";
		}

		public static string StreakNoStreak(string queriedUser, bool winStreak)
		{
			return $"{queriedUser} is not on a streak, last game was a {(winStreak ? "win" : "loss")}.";
		}

		public static string StreakNoGamesFound(string queriedUser)
		{
			return $"Couldn't find games for {queriedUser}.";
		}

		public static string IgnoredUserList(IReadOnlyList<TwitchUser> users)
		{
			if (users.Count == 0) 
			{
				return $"Nobody is ignored :)";
			}

			var userNames = users.Select(u => u.DisplayName).OrderBy(n => n);
			return $"Ignored users: {string.Join(", ", userNames)}";
		}

		public static string UserAddToIgnoreList(string userName)
		{
			return $"User added to the ignore list: {userName}";
		}

		public static string UserRemovedFromIgnoreList(string userName)
		{
			return $"User removed from the ignore list: {userName}";
		}

		public static string ZsemlebotInvalidSetForMessage()
		{
			return $"{Constants.Command_Zsemlebot} was incorrectly formatted. Correct syntax: {Constants.Command_Zsemlebot} setfor <#targetchannel> <targetuser> <option> <newvalue>";
		}

		public static string ZsemlebotInvalidUnSetForMessage()
		{
			return $"{Constants.Command_Zsemlebot} was incorrectly formatted. Correct syntax: {Constants.Command_Zsemlebot} unsetfor <#targetchannel> <targetuser> <option> <newvalue>";
		}
		public static string SayInvalidFormat()
		{
			return $"{Constants.Command_Say} was incorrectly formatted. Correct syntax: {Constants.Command_Say} <#targetchannel> <message>";
		}

		public static string ZsemlebotGetSetting(IReadOnlyList<IReadOnlyZsemlebotSetting> settings)
		{
			var settingTexts = settings
				.OrderBy(s => s.SettingName)
				.Select(s => $"{(s.ChannelTwitchUserId == null ? "(global)" : "")}{s.SettingName}='{s.SettingValue}'");

			return $"Existing settings: {string.Join("; ", settingTexts)}";
		}

		public static string ZsemlebotNoSettingsFound()
		{
			return $"No settings found.";
		}

		public static string ZsemlebotSettingRemoved(string option)
		{
			return $"Setting value for '{option}' is removed.";
		}

		public static string ZsemlebotSettingUpdated(string option)
		{
			return $"Setting value for '{option}' is updated.";
		}

		public static string ZsemlebotCommandDisabled(string command)
		{
			return $"The '{command}' command is now disabled.";
		}

		public static string ZsemlebotCommandEnabled(string command)
		{
			return $"The '{command}' command is now enabled.";
		}

		public static string ZsemlebotInvalidTimeZone(string providedTimeZone)
		{
			return $"The provided timezone is invalid: '{providedTimeZone}'. Valid format: utc[+-]<hour>[:<mins>]. E.g.: utc+5 or utc-6:30";
		}

		public static string StatusMessage(TwitchStatus twitchStatus, DateTime twitchLastMessageAt, HotaClientStatus hotaStatus, DateTime hotaLastMessageAt)
		{
			return $"Status: Twitch: {twitchStatus}; last message at: {twitchLastMessageAt:yyyy-MM-ss HH:mm:ss} ({(DateTime.Now - twitchLastMessageAt).TotalSeconds:0.0} secs ago). Hota: {hotaStatus}; last message at: {hotaLastMessageAt:yyyy-MM-ss HH:mm:ss} ({ (DateTime.Now - hotaLastMessageAt).TotalSeconds:0.0} secs ago)";
		}

		public static string JoinMeInvalidParameter(string senderName)
		{
			return $"{Constants.Command_JoinMe} parameter was not correct. Correct syntax: {Constants.Command_JoinMe} {senderName}";
		}
	}
}
