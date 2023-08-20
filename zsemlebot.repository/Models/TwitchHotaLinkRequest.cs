using System;

namespace zsemlebot.repository.Models
{
    internal class TwitchHotaLinkRequest
    {
        public string TwitchUserName { get; set; }
        public int HotaUserId { get; set; }
        public string AuthCode { get; set; }

        public DateTime ValidUntilUtc { get; set; }
    }
}
