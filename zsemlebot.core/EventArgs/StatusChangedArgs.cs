namespace zsemlebot.core.EventArgs
{
    public class StatusChangedArgs
    {
        public string NewStatus { get; }

        public StatusChangedArgs(string newStatus)
        {
            NewStatus = newStatus;
        }
    }
}
