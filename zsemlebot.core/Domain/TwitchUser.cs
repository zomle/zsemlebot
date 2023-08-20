namespace zsemlebot.core.Domain
{
    public class TwitchUser
    {
        public int TwitchUserId { get; set; }
        public string DisplayName { get; set; }

        public TwitchUser(int twitchUserId, string displayName)
        {
            TwitchUserId = twitchUserId;
            DisplayName = displayName;
        }
    }
}