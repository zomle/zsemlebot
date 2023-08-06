using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            Configuration.Instance.LoadConfig("config.json");

            var twitchService = new TwitchService();
            DataContext = new MainViewModel(twitchService);
        }
    }
}
