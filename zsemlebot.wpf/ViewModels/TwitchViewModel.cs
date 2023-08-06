using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using zsemlebot.core.EventArgs;
using zsemlebot.services;

namespace zsemlebot.wpf.ViewModels
{
    public class TwitchViewModel : ViewModelBase
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

        private string rawCommandText;
        public string RawCommandText
        {
            get { return rawCommandText; }
            set
            {
                if (rawCommandText != value)
                {
                    rawCommandText = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ConnectCommand { get; }
        public ICommand ReconnectCommand { get; }
        public ICommand SendRawCommand { get; }
        public ICommand ClearRawCommand { get; }

        public ObservableCollection<ChatMessage> Messages { get; }

        private TwitchService TwitchService { get; }

        public TwitchViewModel(TwitchService twitchService)
        {
            Messages = new ObservableCollection<ChatMessage>();

            TwitchService = twitchService;

            TwitchService.MessageReceived += TwitchService_MessageReceived;
            TwitchService.StatusChanged += TwitchService_StatusChanged;
            TwitchService.PrivmsgReceived += TwitchService_PrivmsgReceived;

            ConnectCommand = new CommandHandler(
                () =>
                {
                    TwitchService.ConnectToTwitch();
                },
                () => true);

            SendRawCommand = new CommandHandler(
                () =>
                {
                    TwitchService.SendCommand(RawCommandText);
                },
                () => Status == nameof(twitch.Status.Connected));

            ClearRawCommand = new CommandHandler(
               () =>
               {
                   RawCommandText = string.Empty;
               });
        }

        private void TwitchService_StatusChanged(object? sender, StatusChangedArgs e)
        {
            Status = e.NewStatus;
        }

        private void TwitchService_MessageReceived(object? sender, MessageReceivedArgs e)
        {
            LastMessageReceivedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void TwitchService_PrivmsgReceived(object? sender, PrivMsgReceivedArgs e)
        {
            var newMessage = new ChatMessage
            {
                Timestamp = e.Timestamp,
                Channel = e.Channel,
                Sender = e.Sender,
                Message = e.Message
            };

            InvokeOnUI(() => { Messages.Add(newMessage); });

            if (Messages.Count > 200)
            {
                InvokeOnUI(() => { Messages.RemoveAt(0); });
            }
        }
    }
}
