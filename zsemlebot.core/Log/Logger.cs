using System.IO;

namespace zsemlebot.core.Log
{
    public abstract class Logger
    {
        public string FileName { get; }
        public string FilePath { get; }

        public Logger(string fileName)
        {
            FileName = fileName;
            FilePath = Path.Combine(Config.Instance.Global.FullLogDirectory, fileName);
        }

        protected FileStream GetFileStream()
        {
            return new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        protected Logger()
        {
            FileName = string.Empty;
            FilePath = string.Empty;
        }
    }
}
