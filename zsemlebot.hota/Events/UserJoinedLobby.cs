
namespace zsemlebot.hota.Events
{
    public class UserJoinedLobby : HotaEvent
    {
        public int HotaUserId { get; }
        public string UserName { get; }
        public int Elo { get; }
        public int Rep { get; }

        public UserJoinedLobby(int userId, string userName, int userElo, int userRep)
        {
            HotaUserId = userId;
            UserName = userName;
            Elo = userElo;
            Rep = userRep;
        }
    }
}
