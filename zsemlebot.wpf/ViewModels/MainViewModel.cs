using System;
using zsemlebot.services;

namespace zsemlebot.wpf.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public TwitchViewModel TwitchViewModel { get; }

        public MainViewModel(TwitchService twitchService)
        {
            TwitchViewModel = new TwitchViewModel(twitchService);
        }
    }
}
