using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using zsemlebot.services;

namespace zsemlebot.wpf.ViewModels
{
	public class MaintenanceViewModel : ViewModelBase
	{
		public ObservableCollection<ZsemlebotSetting> Settings { get; }
		public ObservableCollection<JoinedChannel> JoinedChannels { get; }
		public ObservableCollection<LinkedTwitchHotaUser> LinkedTwitchUsers { get; }

		public ICommand RefreshViewCommand { get; }
		private BotService BotService { get; }

		public MaintenanceViewModel(BotService botService)
		{
			BotService = botService;

			Settings = new ObservableCollection<ZsemlebotSetting>();
			JoinedChannels = new ObservableCollection<JoinedChannel>();
			LinkedTwitchUsers = new ObservableCollection<LinkedTwitchHotaUser>();

			RefreshViewCommand = new CommandHandler(() =>
			{
				RefreshSettings();
				RefreshJoinedChannels();
				RefreshLinkedUsers();
			});
		}

		private void RefreshSettings()
		{
			var settings = BotService.ListSettings();
			Settings.Clear();

			var tmpList = new List<ZsemlebotSetting>();
			foreach (var setting in settings)
			{
				var item = new ZsemlebotSetting
				{
					TargetTwitchUser = setting.TwitchUser == null ? string.Empty : setting.TwitchUser.DisplayName,
					TargetChannel = setting.TwitchChannel == null ? string.Empty : $"#{setting.TwitchChannel.DisplayName}",
					SettingName = setting.SettingName,
					SettingValue = setting.SettingValue ?? string.Empty
				};
				tmpList.Add(item);
			}

			foreach (var item in tmpList.OrderBy(t => t.TargetTwitchUser).ThenBy(t => t.TargetChannel).ThenBy(t => t.SettingName))
			{
				Settings.Add(item);
			}			
		}

		private void RefreshJoinedChannels()
		{
			var channels = BotService.ListJoinedChannels();
			JoinedChannels.Clear();
			foreach (var channel in channels.OrderBy(c => c.DisplayName))
			{
				JoinedChannels.Add(new JoinedChannel
				{
					Channel = $"#{channel.DisplayName}"
				});
			}
		}

		private void RefreshLinkedUsers()
		{
			var linkedUsers = BotService.ListLinkedUsers();
			LinkedTwitchUsers.Clear();

			var tmp = new List<LinkedTwitchHotaUser>();

			foreach (var linkedTwitchUser in linkedUsers)
			{
				foreach (var linkedHotaUser in linkedTwitchUser.LinkedHotaUsers)
				{
					tmp.Add(new LinkedTwitchHotaUser
					{
						TwitchName = linkedTwitchUser.TwitchUser.DisplayName,
						HotaName = linkedHotaUser.DisplayName
					});
				}
			}

			foreach (var item in tmp.OrderBy(t => t.TwitchName).ThenBy(t => t.HotaName))
			{
				LinkedTwitchUsers.Add(item);
			}
		}
	}
}
