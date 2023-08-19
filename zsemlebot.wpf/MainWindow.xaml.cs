using System;
using System.Windows;
using zsemlebot.core;
using zsemlebot.services;
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

            TwitchService = new TwitchService();
            HotaService = new HotaService();
            BotService = new BotService();

            DataContext = new MainViewModel(TwitchService, HotaService);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TwitchService.Dispose();
            HotaService.Dispose();
            BotService.Dispose();
        }
    }
}
