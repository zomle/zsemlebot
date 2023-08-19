using zsemlebot.hota.Events;

namespace zsemlebot.services
{
    public class HotaUser
    {
        public int HotaUserId { get; set; }
        public string DisplayName { get; set; }
        public int Elo { get; set; }
        public int Rep { get; set; }

        public HotaUser(UserJoinedLobby evnt)
        {
            HotaUserId = evnt.HotaUserId;
            DisplayName = evnt.UserName;
            Elo = evnt.Elo;
            Rep = evnt.Rep;
        }
    }
}