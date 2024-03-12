using System;

namespace zsemlebot.services
{
    internal static class Constants
    {
		/// <summary>
		/// Usage: !ignore <add|del> <twitchusername>
		/// Usage: !ignore list
		/// </summary>
		/// 
		public const string Command_Ignore = "!ignore";

		/// <summary>
		/// Usage: !streak [twitchname|hotaname]
		/// </summary>
		public const string Command_Streak = "!streak";

		/// <summary>
		/// Usage: !today [twitchname|hotaname]
		/// </summary>
		public const string Command_Today = "!today";

		/// <summary>
		/// Usage: !channel <add|del> <#channelname>
		/// </summary>
		public const string Command_Channel = "!channel";

		/// <summary>
		/// Usage: !zsemlebot <enable|disable> <command>
		/// Usage: !zsemlebot set <option> <newvalue>
		/// Usage: !zsemlebot unset <option>
		/// Usage for admin: !zsemlebot setfor <targetchannel> <targetuser> <option> <newvalue>
		/// Usage for admin: !zsemlebot unsetfor <targetchannel> <targetuser> <option>
		/// </summary>
		public const string Command_Zsemlebot = "!zsemlebot";

		/// <summary>
		/// Usage: !leave <twitchname>
		/// </summary>
		public const string Command_Leave = "!leave";

		/// <summary>
		/// Usage 1: !game 
		/// Usage 2: !game edit [template] <hotauser1> [color] [faction] [trade] <hotauser2> [color] [faction] [trade]
		/// Usage 3: !game <twitch/hota user>
		/// </summary>
		public const string Command_Game = "!game";

		/// <summary>
		/// Usage: !linkme <twitchname>
		/// </summary>
		public const string Command_LinkMe = "!linkme";

		/// <summary>
		/// Usage: !link <add|del> twitch=<twitchname> hota=<hotaname>
		/// </summary>
		public const string Command_Link = "!link";

		/// <summary>
		/// Usage: !elo [<twitchname>|<hotaname>]
		/// </summary>
		public const string Command_Elo = "!elo";

		/// <summary>
		/// Usage: !opp
		/// </summary>
		public const string Command_Opp = "!opp";

		/// <summary>
		/// Usage: !rep [<twitchname>|<hotaname>]
		/// </summary>
		public const string Command_Rep = "!rep";

		/// <summary>
		/// This is how much time we will wait before responding to user with the elo value
		/// we already have in the database.
		/// </summary>
		public static readonly TimeSpan RequestEloTimeOut = TimeSpan.FromSeconds(5);

		/// <summary>
		/// This is how much time we will wait before responding to user.
		/// </summary>
		public static readonly TimeSpan RequestRepTimeOut = TimeSpan.FromSeconds(5);

		/// <summary>
		/// This is how much time we will wait before responding to user with
		/// </summary>
		public static readonly TimeSpan RequestGameHistoryTimeOut = TimeSpan.FromSeconds(10);

		public const int UserLinkValidityLengthInMins = 30;

		/// <summary>
		/// This is what we use as "twitch" parameter in commands.
		/// </summary>
		public const string TwitchParameterPrefix = "twitch=";

		/// <summary>
		/// This is what we use as "hota" parameter in commands.
		/// </summary>
		public const string HotaParameterPrefix = "hota=";

		/// <summary>
		/// Number of messages allowed within the time window without triggering spam protection.
		/// </summary>
		public const int SpamProtection_MessageCount = 2;

		/// <summary>
		/// Time window that is checked against number of commands sent.
		/// </summary>
		public static readonly TimeSpan SpamProtection_TimeWindow = TimeSpan.FromSeconds(5);

		public const string Settings_TimeZone = "timezone";
		public const string Settings_CustomElo = "customelo";

		public const string Settings_Enable = "enable";
		public const string Settings_Disable = "disable";
	}
}
