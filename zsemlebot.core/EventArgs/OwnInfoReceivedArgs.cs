namespace zsemlebot.core.EventArgs
{
    public class OwnInfoReceivedArgs
    {
        public OwnInfoReceivedArgs(string userName, int userId)
        {
            DisplayName = userName;
            UserId = userId;
        }

        public string DisplayName { get; set; }
        public int UserId { get; set; }
    }
}
