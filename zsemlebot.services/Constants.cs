using System;

namespace zsemlebot.services
{
    internal static class Constants
    {
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
        /// Usage: !rep [<twitchname>|<hotaname>]
        /// </summary>
        public const string Command_Rep = "!rep";

		/// <summary>
		/// This is how much time we will wait before responding to use with the elo value we already have in the database.
		/// </summary>
		public static readonly TimeSpan RequestEloTimeOut = TimeSpan.FromSeconds(5);

		public const int UserLinkValidityLengthInMins = 30;

		/// <summary>
		/// This is what we use as "twitch" parameter in commands.
		/// </summary>
		public const string TwitchParameterPrefix = "twitch=";

		/// <summary>
		/// This is what we use as "hota" parameter in commands.
		/// </summary>
		public const string HotaParameterPrefix = "hota=";

	}
}
