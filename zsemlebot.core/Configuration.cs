using Newtonsoft.Json;
using System;
using System.IO;

namespace zsemlebot.core
{
    public sealed class Configuration
    {
        public TwitchConfiguration Twitch { get; set; }
        public GlobalConfiguration Global { get; set; }

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

            Instance.Twitch = tmpConfig.Twitch;
            Instance.Global = tmpConfig.Global;

            var fullLogDirectory = Path.Combine(Instance.Global.RootDirectory, Instance.Global.LogDirectory);
            if (!Directory.Exists(fullLogDirectory))
            {
                Directory.CreateDirectory(fullLogDirectory);
            }
        }
    }
}