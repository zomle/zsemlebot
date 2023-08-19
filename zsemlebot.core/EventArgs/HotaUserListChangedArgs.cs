namespace zsemlebot.core.EventArgs
{
    public class HotaUserListChangedArgs
    {
        public int OnlineUserCount { get; }

        public HotaUserListChangedArgs(int onlineUserCount)
        {
            OnlineUserCount = onlineUserCount;
        }
    }
}
