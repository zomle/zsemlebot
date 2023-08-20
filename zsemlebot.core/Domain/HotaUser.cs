namespace zsemlebot.core.Domain
{
    public class HotaUser
    {
        public int HotaUserId { get; set; }
        public string DisplayName { get; set; }
        public int Elo { get; set; }
        public int Rep { get; set; }

        public HotaUser(int hotaUserId, string displayName, int elo, int rep)
        {
            HotaUserId = hotaUserId;
            DisplayName = displayName;
            Elo = elo;
            Rep = rep;
        }
    }
}