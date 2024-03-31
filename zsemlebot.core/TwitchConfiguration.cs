namespace zsemlebot.core
{
    public class TwitchConfiguration
    {
        public string? User { get; set; }
        public string? OAuthToken { get; set; }

        public string? AdminChannel { get; set; }
        public int AdminUserId { get; set; }

		public string? ClientId { get; set; }
		public string? ClientSecret { get; set; }
    }
}