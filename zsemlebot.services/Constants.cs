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
        /// Usage: !elo [<twitchname>|<hotaname>]
        /// </summary>
        public const string Command_Elo = "!elo";

        /// <summary>
        /// Usage: !rep [<twitchname>|<hotaname>]
        /// </summary>
        public const string Command_Rep = "!rep";


        public const int UserLinkValidityLengthInMins = 30;
    }
}
