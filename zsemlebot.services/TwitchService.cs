using System;
using System.Diagnostics;
using System.Threading;
using zsemlebot.core;
using zsemlebot.core.Enums;
using zsemlebot.core.EventArgs;
using zsemlebot.repository;
using zsemlebot.twitch;

namespace zsemlebot.services
{
    public partial class TwitchService : IDisposable
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

        private HotaService HotaService { get; }

        private IrcClient? Client { get; set; }
        private Thread HandleMessagesThread { get; set; }
        private int ReconnectCount { get; set; }

        private static TwitchRepository TwitchRepository { get { return TwitchRepository.Instance; } }
        private static HotaRepository HotaRepository { get { return HotaRepository.Instance; } }
        private static BotRepository BotRepository { get { return BotRepository.Instance; } }

        private static readonly int[] WaitTimesBetweenReconnect = { 0, 1, 2, 4, 8, 16 };

        public TwitchService(HotaService hotaService)
        {
            ReconnectCount = 0;

            HandleMessagesThread = new Thread(HandleMessagesWorker);
            HandleMessagesThread.Start();
            HotaService = hotaService;
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

        public void SendChatMessage(string channel, string message)
        {
            Client?.SendPrivMsg(channel, message);
        }

        public void SendChatMessage(string? parentMessageId, string channel, string message)
        {
            if (parentMessageId == null)
            {
                SendChatMessage(channel, message);
            }
            else
            {
                Client?.SendPrivMsg(parentMessageId, channel, message);
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

            if (userId != null && message.StartsWith('!'))
            {
                var twitchUser = TwitchRepository.GetUser((int)userId);
                var cmdTokens = message.Split(' ', 2);
                var messageId = rawMessage.GetTagValue("id");

				var sender = new MessageSource(twitchUser);
				foreach (var tag in rawMessage.Tags.Values)
				{
					switch (tag.Key)
					{
						case "badges":
							sender.IsVip = tag.Value.Contains("vip");
							sender.IsBroadcaster = tag.Value.Contains("broadcaster");
							break;

						case "mod":
							sender.IsModerator = tag.Value == "1";
							break;
					}
				}

				HandleCommand(messageId, channel, sender, cmdTokens[0], cmdTokens.Length > 1 ? cmdTokens[1] : null);
            }
        }

        private void Client_MessageReceived(object? sender, MessageReceivedArgs e)
        {
            messageReceived?.Invoke(sender, e);
        }

        private void Client_StatusChanged(object? sender, TwitchStatusChangedArgs e)
        {
            statusChanged?.Invoke(sender, e);

            if (e.NewStatus == TwitchStatus.Authenticated)
            {
                var channel = $"#{Config.Instance.Twitch.AdminChannel}";

                Client?.JoinChannel(channel);
                Client?.SendPrivMsg(channel, "Arrived");
            }
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