using System;
using zsemlebot.services;

namespace zsemlebot.wpf.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public TwitchViewModel TwitchViewModel { get; }
        public HotaViewModel HotaViewModel { get; }

        public MainViewModel(TwitchService twitchService, HotaService hotaService)
        {
            TwitchViewModel = new TwitchViewModel(twitchService);
            HotaViewModel = new HotaViewModel(hotaService);
        }
    }
}
