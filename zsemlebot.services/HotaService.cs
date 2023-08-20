using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using zsemlebot.core;
using zsemlebot.core.Domain;
using zsemlebot.core.Enums;
using zsemlebot.core.EventArgs;
using zsemlebot.hota;
using zsemlebot.hota.Events;
using zsemlebot.repository;

namespace zsemlebot.services
{
    public class HotaService : IDisposable
    {
        #region Events
        private EventHandler<MessageReceivedArgs>? messageReceived;
        public event EventHandler<MessageReceivedArgs> MessageReceived
        {
            add { messageReceived += value; }
            remove { messageReceived -= value; }
        }

        private EventHandler<HotaStatusChangedArgs>? statusChanged;
        public event EventHandler<HotaStatusChangedArgs> StatusChanged
        {
            add { statusChanged += value; }
            remove { statusChanged -= value; }
        }

        private EventHandler<HotaUserListChangedArgs>? userListChanged;
        public event EventHandler<HotaUserListChangedArgs> UserListChanged
        {
            add { userListChanged += value; }
            remove { userListChanged -= value; }
        }
        #endregion

        private Dictionary<int, HotaUser> OnlineUsers { get; }
        private LobbyClient? Client { get; set; }
        private Thread HandleMessagesThread { get; set; }
        private int ReconnectCount { get; set; }

        private static HotaRepository HotaRepository { get { return HotaRepository.Instance; } }
        private static BotRepository BotRepository { get { return BotRepository.Instance; } }

        private static readonly int[] WaitTimesBetweenReconnect = { 2, 5, 10, 15, 30 };

        public HotaService()
        {
            ReconnectCount = 0;
            OnlineUsers = new Dictionary<int, HotaUser>(5000);

            HandleMessagesThread = new Thread(HandleMessagesWorker);
            HandleMessagesThread.Start();
        }
        public void Test()
        {
        }

        public bool Connect()
        {
            if (Client != null)
            {
                return true;
            }

            Client = new LobbyClient();
            Client.StatusChanged += Client_StatusChanged;
            Client.MessageReceived += Client_MessageReceived;

            var connected = Client.Connect();
            if (!connected)
            {
                Client.Dispose();
                Client = null;

                statusChanged?.Invoke(this, new HotaStatusChangedArgs(HotaStatus.Initialized));
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
                    if (Client == null || Client.Status == HotaStatus.Initialized)
                    {
                        Thread.Sleep(750);
                        continue;
                    }
                    else if (Client.Status == HotaStatus.Disconnected)
                    {
                        Debug.WriteLine("Hota - HandleMessagesWorker - Disconnected");
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
                    else if (Client.Status == HotaStatus.Connecting)
                    {
                        Debug.WriteLine("Hota - HandleMessagesWorker - Connecting");
                        Thread.Sleep(1000);
                        continue;
                    }

                    while (Client.HasNewEvent())
                    {
                        var newEvent = Client.GetNextEvent();
                        HandleEvent(newEvent);
                    }

                    Thread.Sleep(200);
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

            var tmpClient = new LobbyClient();
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

        public void SendChatMessage(uint targetHotaUserId, string message)
        {
            Client?.SendMessage(targetHotaUserId, message);
        }

        private TimeSpan GetWaitBetweenReconnects()
        {
            int index = ReconnectCount >= WaitTimesBetweenReconnect.Length ? WaitTimesBetweenReconnect.Length - 1 : ReconnectCount;
            return TimeSpan.FromSeconds(WaitTimesBetweenReconnect[index]);
        }

        private void HandleEvent(HotaEvent? hotaEvent)
        {
            if (hotaEvent == null)
            {
                return;
            }

            switch (hotaEvent)
            {
                case UserJoinedLobby ujl:
                    HandleUserJoinedLobby(ujl);
                    break;

                case UserLeftLobby ull:
                    HandleUserLeftLobby(ull);
                    break;

                case IncomingMessage im:
                    HandleIncomingMessage(im);
                    break;
            }
        }

        private void HandleUserJoinedLobby(UserJoinedLobby evnt)
        {
            var hotaUser = new HotaUser(evnt.HotaUserId, evnt.UserName, evnt.Elo, evnt.Rep);

            OnlineUsers[evnt.HotaUserId] = hotaUser;
            HotaRepository.UpdateHotaUser(hotaUser);

            userListChanged?.Invoke(this, new HotaUserListChangedArgs(OnlineUsers.Count));
        }

        private void HandleUserLeftLobby(UserLeftLobby evnt)
        {
            OnlineUsers.Remove(evnt.HotaUserId);

            userListChanged?.Invoke(this, new HotaUserListChangedArgs(OnlineUsers.Count));
        }

        private void HandleIncomingMessage(IncomingMessage evnt)
        {
            if (!evnt.Message.StartsWith('!'))
            {
                return;
            }

            var tokens = evnt.Message.Split(' ', 2);
            HandleCommand(evnt.SourceUserId, tokens[0], tokens.Length > 1 ? tokens[1] : null);
        }

        private void HandleCommand(int sourceUserId, string command, string? parameters)
        {
            var hotaUser = HotaRepository.GetUser(sourceUserId);
            if (hotaUser == null)
            {
                return;
            }

            switch (command)
            {
                case Constants.Command_LinkMe:
                    HandleLinkMeCommand(hotaUser, parameters);
                    break;
            }
        }

        private void HandleLinkMeCommand(HotaUser source, string? parameters)
        {
            if (parameters == null)
            {
                return;
            }

            string twitchUserName = parameters;

            //get existing user link request, if any
            var existingRequest = BotRepository.GetUserLinkRequest(source.HotaUserId, twitchUserName);
            if (existingRequest != null)
            {
                //if it's not valid anymore, delete it
                if (existingRequest.ValidUntilUtc < DateTime.UtcNow)
                {
                    BotRepository.DeleteUserLinkRequest(source.HotaUserId, twitchUserName);
                    existingRequest = null;
                }
            }

            string authCode;
            //if a valid link request exists, update the timer and get the auth code, otherwise create a new request
            if (existingRequest != null)
            {
                BotRepository.UpdateUserLinkRequest(existingRequest, Constants.UserLinkValidityLengthInMins);
                authCode = existingRequest.AuthCode;
            }
            else
            {
                authCode = RandomGenerator.GenerateCode();
                BotRepository.CreateUserLinkRequest(source.HotaUserId, twitchUserName, authCode, Constants.UserLinkValidityLengthInMins);
            }

            //send a message to the user
            SendChatMessage((uint)source.HotaUserId, string.Format(Constants.Message_UserLinkLobbyMessage, authCode, twitchUserName, Config.Instance.Twitch.AdminChannel));
        }

        private void Client_MessageReceived(object? sender, MessageReceivedArgs e)
        {
            messageReceived?.Invoke(sender, e);
        }

        private void Client_StatusChanged(object? sender, HotaStatusChangedArgs e)
        {
            statusChanged?.Invoke(sender, e);
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