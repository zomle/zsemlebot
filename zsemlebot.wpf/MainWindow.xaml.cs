using System;
using System.Windows;
using zsemlebot.core;
using zsemlebot.repository;
using zsemlebot.services;
using zsemlebot.services.Log;
using zsemlebot.wpf.ViewModels;

namespace zsemlebot.wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TwitchService TwitchService { get; }
        private HotaService HotaService { get; }
        private BotService BotService { get; }

        public MainWindow()
        {
            InitializeComponent();

            Config.Instance.LoadConfig("config.json");

            BotLogger.Instance.LogEvent(BotLogSource.Intrnl, $"Config loaded. Twitch user: {Config.Instance.Twitch.User}; Hota user: {Config.Instance.Hota.User}; Hota client version: {Config.Instance.Hota.ClientVersion}");

            HotaService = new HotaService();
            TwitchService = new TwitchService(HotaService);
            BotService = new BotService();

            DataContext = new MainViewModel(TwitchService, HotaService, BotService);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TwitchService.Dispose();
            HotaService.Dispose();
            BotService.Dispose();

			ZsemlebotRepositoryBase.DisposeStatic();
        }
    }
}
