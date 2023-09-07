using System;

namespace zsemlebot.repository.Models
{
    internal class TwitchHotaLinkRequestData
    {
        public string TwitchUserName { get; set; }
        public uint HotaUserId { get; set; }
        public string AuthCode { get; set; }

        public DateTime ValidUntilUtc { get; set; }
    }
}
