
namespace zsemlebot.hota.Events
{
    public class UserJoinedLobby : HotaEvent
    {
        public uint HotaUserId { get; }
        public string UserName { get; }
        public int Elo { get; }
        public int Rep { get; }
        public short Status { get; }

        public UserJoinedLobby(uint userId, string userName, int userElo, int userRep, short status)
        {
            HotaUserId = userId;
            UserName = userName;
            Elo = userElo;
            Rep = userRep;
            Status = status;
        }
    }
}
