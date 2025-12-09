using System;
using zsemlebot.services;

namespace zsemlebot.wpf.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public TwitchViewModel TwitchViewModel { get; }
        public HotaViewModel HotaViewModel { get; }
		public MaintenanceViewModel MaintenanceViewModel { get; }

        public MainViewModel(TwitchService twitchService, HotaService hotaService, BotService botService)
        {
            TwitchViewModel = new TwitchViewModel(twitchService);
            HotaViewModel = new HotaViewModel(hotaService, twitchService);
			MaintenanceViewModel = new MaintenanceViewModel(botService);
        }
    }
}
