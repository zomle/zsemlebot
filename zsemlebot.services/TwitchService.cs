using System;
using System.Diagnostics;
using System.Threading;
using zsemlebot.twitch;

namespace zsemlebot.services
{
    public class TwitchService
    {
        private static readonly int[] WaitTimesBetweenReconnect = { 0, 1, 2, 4, 8, 16 };
        private IrcClient Client { get; set; }
        private int ReconnectCount { get; set; }

        public TwitchService()
        {
            ReconnectCount = 0;
        }

        public void ConnectToTwitch()
        {
            if (Client == null)
            {
                Client = new IrcClient();
            }
            Client.Connect();
            new Thread(HandleMessagesWorker).Start();
        }

        public void JoinAndTalk()
        {
        }

        public void HandleMessagesWorker()
        {
            while (true)
            {
                if (Client.Status == Status.Initialized)
                {
                    Debug.WriteLine("HandleMessagesWorker - Initialized");
                    Thread.Sleep(750);
                    continue;
                }
                else if (Client.Status == Status.Disconnected)
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
                else if (Client.Status == Status.Connecting)
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
            var tmpClient = new IrcClient();
            var reconnected = tmpClient.Connect();
            if (reconnected)
            {
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
                
            }
        }
    }
}