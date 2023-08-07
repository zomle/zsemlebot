using Newtonsoft.Json;
using System;
using System.IO;

namespace zsemlebot.core
{
    public sealed class Config
    {
        public GlobalConfiguration Global { get; set; }
        public TwitchConfiguration Twitch { get; set; }
        public HotaConfiguration Hota { get; set; }

        private static readonly Config instance;

        public static Config Instance { get { return instance; } }

        private Config()
        {
            Global = new GlobalConfiguration();
            Twitch = new TwitchConfiguration();
            Hota = new HotaConfiguration();
        }

        static Config()
        {
            instance = new Config();
        }

        public void LoadConfig(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Config file doesn't exist: '{filePath}'");
            }
            var content = File.ReadAllText(filePath);
            var tmpConfig = JsonConvert.DeserializeObject<Config>(content);

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