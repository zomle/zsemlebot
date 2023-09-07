using System.Linq;

namespace zsemlebot.hota.Events
{
    public class UserLeftLobby : HotaEvent
    {
        public uint HotaUserId { get; }

        public UserLeftLobby(uint userId)
        {
            HotaUserId = userId;
        }
    }
}
