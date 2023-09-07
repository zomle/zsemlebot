namespace zsemlebot.core.EventArgs
{
    public class OwnInfoReceivedArgs
    {
        public OwnInfoReceivedArgs(string userName, uint userId)
        {
            DisplayName = userName;
            UserId = userId;
        }

        public string DisplayName { get; set; }
        public uint UserId { get; set; }
    }
}
