
namespace zsemlebot.hota.Events
{
    public class UserStatusChange : HotaEvent
    {
        public uint HotaUserId { get; }
        public short NewStatus { get; }

        public UserStatusChange(uint hotaUserId, short newStatus)
        {
            HotaUserId = hotaUserId;
            NewStatus = newStatus;
        }
    }
}
