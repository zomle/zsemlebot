namespace zsemlebot.core.Log
{
    public class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();

        private NullLogger()
        {
        }

        public void Write(string text)
        {
        }

        public void WriteLine(string text)
        {
        }
    }
}
