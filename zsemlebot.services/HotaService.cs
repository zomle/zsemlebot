using System;
using System.Diagnostics;
using System.Threading;
using zsemlebot.core.Enums;
using zsemlebot.core.EventArgs;
using zsemlebot.hota;

namespace zsemlebot.services
{
    public class HotaService
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
        #endregion

        private LobbyClient? Client { get; set; }
        private Thread HandleMessagesThread { get; set; }
        private int ReconnectCount { get; set; }

        private static readonly int[] WaitTimesBetweenReconnect = { 0, 1, 2, 4, 8, 16 };

        public HotaService()
        {
            ReconnectCount = 0;

            HandleMessagesThread = new Thread(HandleMessagesWorker);
            HandleMessagesThread.Start();
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

                Thread.Sleep(500);
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

        private void Client_MessageReceived(object? sender, MessageReceivedArgs e)
        {
            messageReceived?.Invoke(sender, e);
        }

        private void Client_StatusChanged(object? sender, HotaStatusChangedArgs e)
        {
            statusChanged?.Invoke(sender, e);
        }
    }
}