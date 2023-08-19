using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using zsemlebot.core.Enums;
using zsemlebot.core.EventArgs;
using zsemlebot.hota;
using zsemlebot.hota.Events;
using zsemlebot.repository;
using zsemlebot.twitch;

namespace zsemlebot.services
{
    public class HotaUser
    {
        public int HotaUserId { get; set; }
        public string DisplayName { get; set; }
        public int Elo { get; set; }
        public int Rep { get; set; }

        public HotaUser(UserJoinedLobby evnt)
        {
            HotaUserId = evnt.HotaUserId;
            DisplayName = evnt.UserName;
            Elo = evnt.Elo;
            Rep = evnt.Rep;
        }
    }

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

        private HotaRepository HotaRepository { get; set; }

        private static readonly int[] WaitTimesBetweenReconnect = { 2, 5, 10, 15, 30 };

        public HotaService()
        {
            ReconnectCount = 0;
            OnlineUsers = new Dictionary<int, HotaUser>(5000);

            HotaRepository = new HotaRepository();

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
            }
        }

        private void HandleUserJoinedLobby(UserJoinedLobby evnt)
        {
            OnlineUsers[evnt.HotaUserId] = new HotaUser(evnt);
            HotaRepository.UpdateHotaUser(evnt.HotaUserId, evnt.UserName, evnt.Elo, evnt.Rep);

            userListChanged?.Invoke(this, new HotaUserListChangedArgs(OnlineUsers.Count));
        }

        private void HandleUserLeftLobby(UserLeftLobby evnt)
        {
            OnlineUsers.Remove(evnt.HotaUserId);

            userListChanged?.Invoke(this, new HotaUserListChangedArgs(OnlineUsers.Count));
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