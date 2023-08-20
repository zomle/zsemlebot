using System;

namespace zsemlebot.services
{
    internal static class Constants
    {
        /// <summary>
        /// Usage: !linkme <twitchname>
        /// </summary>
        public const string Command_LinkMe = "!linkme";


        public const int UserLinkValidityLengthInMins = 30;


        /// <summary>
        /// First argument is 'Auth Code', second argument is 'Twitch username', third argument is admin channel name.
        /// </summary>
        public const string Message_UserLinkLobbyMessage = "Auth code: {0}. Send '!linkme {0}' as '{1}' on twitch in '{2}' channel chat.";

        /// <summary>
        /// First argument is 'Twitch username', second argument is 'Hota username'
        /// </summary>
        public const string Message_UserLinkTwitchMessage = "Your twitch user '{0}' is now linked with your hota lobby user '{1}'.";
    }
}
