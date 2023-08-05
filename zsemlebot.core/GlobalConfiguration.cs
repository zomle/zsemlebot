using System.IO;
using System.Reflection;

namespace zsemlebot.core
{
    public class GlobalConfiguration
    {
        public string LogDirectory { get; set; }
        public string RootDirectory
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }
    }
}