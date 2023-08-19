
namespace zsemlebot.hota.Events
{
    public class UserLeftLobby : HotaEvent
    {
        public int HotaUserId { get; }

        public UserLeftLobby(int userId)
        {
            HotaUserId = userId;
        }
    }
}
