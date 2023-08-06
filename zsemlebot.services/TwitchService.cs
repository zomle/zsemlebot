using System;
using System.Diagnostics;
using System.Threading;
using zsemlebot.core.EventArgs;
using zsemlebot.repository;
using zsemlebot.twitch;

namespace zsemlebot.services
{
    public class TwitchService 
    {
        private EventHandler<MessageReceivedArgs>? messageReceived;
        public event EventHandler<MessageReceivedArgs> MessageReceived
        {
            add { messageReceived += value; }
            remove { messageReceived -= value; }
        }

        private EventHandler<StatusChangedArgs>? statusChanged;
        public event EventHandler<StatusChangedArgs> StatusChanged
        {
            add { statusChanged += value; }
            remove { statusChanged -= value; }
        }

        private EventHandler<PrivMsgReceivedArgs>? privmsgReceived;
        public event EventHandler<PrivMsgReceivedArgs> PrivmsgReceived
        {
            add { privmsgReceived += value; }
            remove { privmsgReceived -= value; }
        }

        private IrcClient Client { get; set; }
        private int ReconnectCount { get; set; }

        private TwitchRepository TwitchRepository { get; set; }

        private static readonly int[] WaitTimesBetweenReconnect = { 0, 1, 2, 4, 8, 16 };

        public TwitchService()
        {
            ReconnectCount = 0;

            TwitchRepository = new TwitchRepository();
        }

        public void ConnectToTwitch()
        {
            if (Client == null)
            {
                Client = new IrcClient();
                Client.StatusChanged += Client_StatusChanged;
                Client.MessageReceived += Client_MessageReceived;

            }
            Client.Connect();
            new Thread(HandleMessagesWorker).Start();
        }

        public void JoinAndTalk()
        {
            Client.JoinChannel("#zomle");
            Client.SendPrivMsg("#zomle", "peepoGlad");
        }

        public void HandleMessagesWorker()
        {
            while (true)
            {
                if (Client.Status == TwitchStatus.Initialized)
                {
                    Debug.WriteLine("HandleMessagesWorker - Initialized");
                    Thread.Sleep(750);
                    continue;
                }
                else if (Client.Status == TwitchStatus.Disconnected)
                {
                    Debug.WriteLine("HandleMessagesWorker - Disconnected");
                    if (!Reconnect())
                    {
                        ReconnectCount++;
                        Thread.Sleep(GetWaitBetweenReconnects());
                    }
                    else
                    {
                        ReconnectCount = 0;
                    }
                    continue;
                }
                else if (Client.Status == TwitchStatus.Connecting)
                {
                    Debug.WriteLine("HandleMessagesWorker - Connecting");
                    Thread.Sleep(1000);
                    continue;
                }

                if (!Client.HasNewMessage()) {
                    Thread.Sleep(100);
                    continue;
                }

                var message = Client.GetNextMessage();
                HandleMessage(message);
            }
        }

        private bool Reconnect()
        {
            //todo tear down previous connection if there is one active.

            var tmpClient = new IrcClient();
            tmpClient.StatusChanged += Client_StatusChanged;
            tmpClient.MessageReceived += Client_MessageReceived;

            var reconnected = tmpClient.Connect();
            if (reconnected)
            {
                Client.StatusChanged -= Client_StatusChanged;
                Client.MessageReceived -= Client_MessageReceived;
                Client = tmpClient;
                return true;
            }
            else
            {
                return false;
            }
        }

        private TimeSpan GetWaitBetweenReconnects()
        {
            int index = ReconnectCount >= WaitTimesBetweenReconnect.Length ? WaitTimesBetweenReconnect.Length - 1 : ReconnectCount;
            return TimeSpan.FromSeconds(WaitTimesBetweenReconnect[index]);
        }

        private void HandleMessage(Message message)
        {
            switch (message.Command)
            {
                case "PRIVMSG":
                    HandlePrivMsg(message);
                    break;
            }
        }

        private void HandlePrivMsg(Message rawMessage)
        {
            var displayName = rawMessage.SourceUserName;
            var userId = rawMessage.SourceUserId;

            if (displayName != null && userId != 0)
            {
                TwitchRepository.UpdateTwitchUserName(userId, displayName); 
            }

            var tokens = rawMessage.Params.Split(' ', 2);
            var channel = tokens[0];
            var message = tokens[1][1..];

            privmsgReceived?.Invoke(this, new PrivMsgReceivedArgs(channel, displayName, message));
        }

        private void Client_MessageReceived(object? sender, MessageReceivedArgs e)
        {
            messageReceived?.Invoke(sender, e);
        }

        private void Client_StatusChanged(object? sender, StatusChangedArgs e)
        {
            statusChanged?.Invoke(sender, e);
        }

        public void SendCommand(string rawCommandText)
        {
            Client?.SendRawCommand(rawCommandText);
        }
    }
}