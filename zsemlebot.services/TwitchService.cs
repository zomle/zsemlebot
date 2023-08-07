using System;
using System.Diagnostics;
using System.Threading;
using zsemlebot.core.Enums;
using zsemlebot.core.EventArgs;
using zsemlebot.repository;
using zsemlebot.twitch;

namespace zsemlebot.services
{
    public class TwitchService : IDisposable
    {
        #region Events
        private EventHandler<MessageReceivedArgs>? messageReceived;
        public event EventHandler<MessageReceivedArgs> MessageReceived
        {
            add { messageReceived += value; }
            remove { messageReceived -= value; }
        }

        private EventHandler<TwitchStatusChangedArgs>? statusChanged;
        public event EventHandler<TwitchStatusChangedArgs> StatusChanged
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
        #endregion

        private IrcClient? Client { get; set; }
        private Thread HandleMessagesThread { get; set; }
        private int ReconnectCount { get; set; }

        private TwitchRepository TwitchRepository { get; set; }

        private static readonly int[] WaitTimesBetweenReconnect = { 0, 1, 2, 4, 8, 16 };

        public TwitchService()
        {
            ReconnectCount = 0;

            TwitchRepository = new TwitchRepository();

            HandleMessagesThread = new Thread(HandleMessagesWorker);
            HandleMessagesThread.Start();
        }

        public bool Connect()
        {
            if (Client != null)
            {
                return true;
            }

            Client = new IrcClient();
            Client.StatusChanged += Client_StatusChanged;
            Client.MessageReceived += Client_MessageReceived;

            var connected = Client.Connect();
            if (!connected)
            {
                Client.Dispose();
                Client = null;

                statusChanged?.Invoke(this, new TwitchStatusChangedArgs(TwitchStatus.Initialized));
                return false;
            }
            return true;
        }


        public void HandleMessagesWorker()
        {
            try
            {
                while (true)
                {
                    if (Client == null || Client.Status == TwitchStatus.Initialized)
                    {
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

                    if (!Client.HasNewMessage())
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    var message = Client.GetNextMessage();
                    HandleMessage(message);
                }
            }
            catch (ThreadInterruptedException)
            {
            }
        }

        public bool Reconnect()
        {
            if (Client == null)
            {
                return Connect();
            }

            var tmpClient = new IrcClient();
            tmpClient.StatusChanged += Client_StatusChanged;
            tmpClient.MessageReceived += Client_MessageReceived;

            var reconnected = tmpClient.Connect();
            if (reconnected)
            {
                Client.StatusChanged -= Client_StatusChanged;
                Client.MessageReceived -= Client_MessageReceived;
                Client.Dispose();

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

        private void HandleMessage(Message? message)
        {
            if (message == null)
            {
                return;
            }

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

            if (displayName != null && userId != null)
            {
                TwitchRepository.UpdateTwitchUserName(userId.Value, displayName);
            }

            var tokens = rawMessage.Params.Split(' ', 2);
            var channel = tokens[0];
            var message = tokens[1][1..];

            privmsgReceived?.Invoke(this, new PrivMsgReceivedArgs(channel, displayName ?? "-", message));
        }

        private void Client_MessageReceived(object? sender, MessageReceivedArgs e)
        {
            messageReceived?.Invoke(sender, e);
        }

        private void Client_StatusChanged(object? sender, TwitchStatusChangedArgs e)
        {
            statusChanged?.Invoke(sender, e);
        }

        public void SendCommand(string rawCommandText)
        {
            Client?.SendRawCommand(rawCommandText);
        }

        #region IDisposable implementation
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        HandleMessagesThread?.Interrupt();
                    }
                    catch { }

                    try
                    {
                        if (Client != null)
                        {
                            Client.StatusChanged -= Client_StatusChanged;
                            Client.MessageReceived -= Client_MessageReceived;
                            Client.Dispose();
                        }
                    }
                    catch { }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}