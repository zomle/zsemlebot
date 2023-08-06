using Newtonsoft.Json;
using System;
using System.IO;

namespace zsemlebot.core
{
    public sealed class Configuration
    {
        public GlobalConfiguration Global { get; set; }
        public TwitchConfiguration Twitch { get; set; }
        public HotaConfiguration Hota { get; set; }


        private static readonly Configuration instance;

        public static Configuration Instance { get { return instance; } }

        private Configuration()
        {
        }

        static Configuration()
        {
            instance = new Configuration();
        }

        public void LoadConfig(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Config file doesn't exist: '{filePath}'");
            }
            var content = File.ReadAllText(filePath);
            var tmpConfig = JsonConvert.DeserializeObject<Configuration>(content);

            if (tmpConfig == null)
            {
                throw new InvalidOperationException($"Failed to deserialize config file.");
            }

            Instance.Global = tmpConfig.Global;
            Instance.Twitch = tmpConfig.Twitch;
            Instance.Hota = tmpConfig.Hota;

            CreateDirectory(Instance.Global.LogDirectory);
            CreateDirectory(Instance.Global.DbDirectory);
            CreateDirectory(Instance.Global.DbBackupDirectory);
        }

        private void CreateDirectory(string relativeDir)
        {
            var fullDirectoryPath = Path.Combine(Instance.Global.RootDirectory, relativeDir);
            if (!Directory.Exists(fullDirectoryPath))
            {
                Directory.CreateDirectory(fullDirectoryPath);
            }
        }
    }
}