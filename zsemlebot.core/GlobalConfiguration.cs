using System.IO;
using System.Reflection;

namespace zsemlebot.core
{
    public class GlobalConfiguration
    {
        public string DatabaseFileName { get { return "zsemlebot.db3"; } }
        public string FullDatabaseFilePath { get { return Path.Combine(FullDbDirectory, DatabaseFileName); } }

        public string LogDirectory { get; set; }
        public string FullLogDirectory { get { return Path.Combine(RootDirectory, LogDirectory); } }

        public string DbDirectory { get; set; }
        public string FullDbDirectory { get { return Path.Combine(RootDirectory, DbDirectory); } }

        public string DbBackupDirectory { get; set; }
        public string FullDbBackupDirectory { get { return Path.Combine(RootDirectory, DbBackupDirectory); } }

        public string RootDirectory { get; }

        public GlobalConfiguration()
        {
            RootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

            LogDirectory = string.Empty;
            DbDirectory = string.Empty;
            DbBackupDirectory = string.Empty;
        }
    }
}