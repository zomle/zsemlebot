using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using zsemlebot.core;
using zsemlebot.services;
using zsemlebot.twitch;

namespace zsemlebot.wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TwitchService Twitch { get; set; }

        public MainWindow()
        {
            InitializeComponent();

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Configuration.Instance.LoadConfig("config.json");
            
            Twitch = new TwitchService();
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            Twitch.ConnectToTwitch();
        }

        private void Replay_Click(object sender, RoutedEventArgs e)
        {
            Twitch.JoinAndTalk();
        }
    }
}
