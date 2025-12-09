using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using zsemlebot.core;
using zsemlebot.core.Enums;
using zsemlebot.core.EventArgs;
using zsemlebot.services;
using zsemlebot.twitch;

namespace zsemlebot.wpf.ViewModels
{
	public class HotaViewModel : ViewModelBase
	{
		private string status;
		public string Status
		{
			get { return status; }
			set
			{
				if (status != value)
				{
					status = value;
					OnPropertyChanged();
				}
			}
		}

		private string lastMessageReceivedAt;
		public string LastMessageReceivedAt
		{
			get { return lastMessageReceivedAt; }
			set
			{
				if (lastMessageReceivedAt != value)
				{
					lastMessageReceivedAt = value;
					OnPropertyChanged();
				}
			}
		}

		private string lastPingSentAt;
		public string LastPingSentAt
		{
			get { return lastPingSentAt; }
			set
			{
				if (lastPingSentAt != value)
				{
					lastPingSentAt = value;
					OnPropertyChanged();
				}
			}
		}

		public int onlineUserCount;
		public int OnlineUserCount
		{
			get { return onlineUserCount; }
			set
			{
				if (onlineUserCount != value)
				{
					onlineUserCount = value;
					OnPropertyChanged();
				}
			}
		}
		
		public int gamesNotFull;
		public int GamesNotFull
		{
			get { return gamesNotFull; }
			set
			{
				if (gamesNotFull != value)
				{
					gamesNotFull = value;
					OnPropertyChanged();
				}
			}
		}

		public int gamesNotStarted;
		public int GamesNotStarted
		{
			get { return gamesNotStarted; }
			set
			{
				if (gamesNotStarted != value)
				{
					gamesNotStarted = value;
					OnPropertyChanged();
				}
			}
		}

		public int gamesInProgress;
		public int GamesInProgress
		{
			get { return gamesInProgress; }
			set
			{
				if (gamesInProgress != value)
				{
					gamesInProgress = value;
					OnPropertyChanged();
				}
			}
		}

		private uint clientVersion;
		public uint ClientVersion
		{
			get { return clientVersion; }
			set
			{
				if (clientVersion != value)
				{
					clientVersion = value;
					OnPropertyChanged();
				}
			}
		}

		public ICommand ConnectCommand { get; }
		public ICommand ReconnectCommand { get; }
		public ICommand TestCommand { get; }

		public ObservableCollection<ChatMessage> Messages { get; }

		private HotaService HotaService { get; }
		private TwitchService TwitchService { get; }

		public HotaViewModel(HotaService hotaService, TwitchService twitchService)
		{
			lastMessageReceivedAt = "-";
			lastPingSentAt = "-";

			status = nameof(HotaClientStatus.Initialized);

			clientVersion = Config.Instance.Hota.ClientVersion;

			HotaService = hotaService;
			TwitchService = twitchService;

			HotaService.PrivmsgReceived += HotaService_PrivmsgReceived;
			HotaService.MessageReceived += HotaService_MessageReceived;
			HotaService.PingSent += HotaService_PingSent;
			HotaService.StatusChanged += HotaService_StatusChanged;
			HotaService.UserListChanged += HotaService_UserListChanged;
			HotaService.GameListChanged += HotaService_GameListChanged;

			Messages = new ObservableCollection<ChatMessage>();

			ConnectCommand = new CommandHandler(
				() => HotaService.Connect(),
				() => Status == nameof(HotaClientStatus.Initialized));

			ReconnectCommand = new CommandHandler(
				() => HotaService.Reconnect(),
				() => Status != nameof(HotaClientStatus.Initialized));

			TestCommand = new CommandHandler(
				() => HotaService.Test());
		}

		private void HotaService_GameListChanged(object? sender, HotaGameListChangedArgs e)
		{
			GamesInProgress = e.GamesInProgress;
			GamesNotStarted = e.GamesNotStarted;
			GamesNotFull = e.GamesNotFull;
		}

		private void HotaService_UserListChanged(object? sender, HotaUserListChangedArgs e)
		{
			OnlineUserCount = e.OnlineUserCount;
		}

		private void HotaService_StatusChanged(object? sender, HotaStatusChangedArgs e)
		{
			if (e.MinimumClientVersion == null)
			{
				Status = e.NewStatus.ToString();
			}
			else
			{
				Status = $"{e.NewStatus} (min version: {e.MinimumClientVersion})";

				if (e.NewStatus == HotaClientStatus.ObsoleteClient)
				{
					var adminUser = TwitchService.GetUser(Config.Instance.Twitch.AdminUserId);
					var channel = $"#{Config.Instance.Twitch.AdminChannel}";

					TwitchService.SendChatMessage(channel, $"{(adminUser == null ? "" : "@" + adminUser.DisplayName)} Lobby updated, new minimum client version: {e.MinimumClientVersion}");
				}
			}
		}

		private void HotaService_MessageReceived(object? sender, MessageReceivedArgs e)
		{
			LastMessageReceivedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
		}

		private void HotaService_PingSent(object? sender, PingSentArgs e)
		{
			LastPingSentAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
		}

		private void HotaService_PrivmsgReceived(object? sender, PrivMsgReceivedArgs e)
		{
			var newMessage = new ChatMessage(e.Timestamp, e.Target, e.Sender, e.Message);

			InvokeOnUI(() => { Messages.Add(newMessage); });

			if (Messages.Count > 200)
			{
				InvokeOnUI(() => { Messages.RemoveAt(0); });
			}
		}
	}
}
