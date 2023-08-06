namespace zsemlebot.core
{
    public class HotaConfiguration
    {
        public string ServerAddress { get; set; }
        public ushort ServerPort { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public uint ClientVersion { get; set; }
    }
}