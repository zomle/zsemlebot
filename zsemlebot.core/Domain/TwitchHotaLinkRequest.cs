using System;

namespace zsemlebot.core.Domain
{
    public class TwitchHotaLinkRequest
    {
        public string TwitchUserName { get; set; }
        public uint HotaUserId { get; set; }
        public string AuthCode { get; set; }

        public DateTime ValidUntilUtc { get; set; }
    }
}
