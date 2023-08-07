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
        public MainWindow()
        {
            InitializeComponent();

            Config.Instance.LoadConfig("config.json");

            var twitchService = new TwitchService();
            var hotaService = new HotaService();
            DataContext = new MainViewModel(twitchService, hotaService);
        }
    }
}
