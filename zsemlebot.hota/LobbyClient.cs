using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using zsemlebot.core;
using zsemlebot.core.Enums;
using zsemlebot.core.EventArgs;
using zsemlebot.hota.Log;
using zsemlebot.hota.Messages;
using zsemlebot.networklib;

namespace zsemlebot.hota
{
    public class LobbyClient : IDisposable
    {
        private readonly TimeSpan PingFrequency = TimeSpan.FromSeconds(30);

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

        private HotaStatus status;
        public HotaStatus Status
        {
            get { return status; }
            private set
            {
                if (status != value)
                {
                    status = value;
                    statusChanged?.Invoke(this, new HotaStatusChangedArgs(value));
                }
            }
        }

        private Socket? Socket { get; set; }

        private Thread? ReadThread { get; set; }
        private Thread? PingThread { get; set; }

        private Queue<HotaMessageBase> IncomingMessageQueue { get; set; }
        private HotaRawLogger RawLogger { get; set; }
        private HotaPackageLogger PackageLogger { get; set; }
        private HotaEventLogger EventLogger { get; set; }

        private DateTime lastMessageReceivedAt;
        private DateTime LastMessageReceivedAt
        {
            get { return lastMessageReceivedAt; }
            set
            {
                if (lastMessageReceivedAt != value)
                {
                    lastMessageReceivedAt = value;
                    messageReceived?.Invoke(this, new MessageReceivedArgs());
                }
            }
        }

        private DateTime LastPingSentAt { get; set; }

        public LobbyClient()
        {
            IncomingMessageQueue = new Queue<HotaMessageBase>();
            Status = HotaStatus.Initialized;

            RawLogger = HotaRawLogger.Null;
            EventLogger = HotaEventLogger.Null;
            PackageLogger = HotaPackageLogger.Null;
        }

        public bool Connect()
        {
            if (Config.Instance.Hota.ServerAddress == null)
            {
                return false;
            }

            var now = DateTime.Now;

            Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            Status = HotaStatus.Connecting;

            try
            {
                Socket.Connect(Config.Instance.Hota.ServerAddress, Config.Instance.Hota.ServerPort);

                RawLogger = new HotaRawLogger(now);
                EventLogger = new HotaEventLogger(now);
                PackageLogger = new HotaPackageLogger(now);

                IncomingMessageQueue.Clear();

                Status = HotaStatus.Connected;

                SendLoginMessage();

                lastMessageReceivedAt = DateTime.Now;

                ReadThread = new Thread(ReadThreadWorker);
                ReadThread.Start();

                return true;
            }
            catch (Exception)
            {
                Status = HotaStatus.Disconnected;
                return false;
            }
        }
        private void StartPingThread()
        {
            PingThread = new Thread(PingThreadWorker);
            PingThread.Start();
        }

        private void PingThreadWorker()
        {
            try
            {
                while (true)
                {
                    var timeSinceLastMessage = DateTime.Now - LastMessageReceivedAt;
                    bool noMessagesForAWhile = timeSinceLastMessage > PingFrequency;
                    bool shouldPingAgain = LastMessageReceivedAt > LastPingSentAt || (DateTime.Now - LastPingSentAt > PingFrequency);

                    if (Status == HotaStatus.Connected && noMessagesForAWhile && shouldPingAgain)
                    {
                        LastPingSentAt = DateTime.Now;
                        SendPing();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                }
            }
            catch (Exception)
            {
                Status = HotaStatus.Disconnected;
            }
        }

        private void SendLoginMessage()
        {
            if (string.IsNullOrEmpty(Config.Instance.Hota.User)
                || string.IsNullOrEmpty(Config.Instance.Hota.Password))
            {
                return;
            }

            var message = new LoginMessage(Config.Instance.Hota.User, Config.Instance.Hota.Password, Config.Instance.Hota.ClientVersion);
            SendSocketRaw(message);
        }
        private void SendPing()
        {
            SendSocketRaw(new MaybePingMessage());
        }

        private void SendSocketRaw(HotaMessageBase message)
        {
            if (Socket == null)
            {
                return;
            }

            var package = message.AsDataPackage();
            PackageLogger.LogPackage(false, package);

            int sent = 0;
            var bytes = package.Content;
            while (sent != bytes.Length)
            {
                var tmp = Socket.Send(bytes, sent, bytes.Length - sent, SocketFlags.None);
                sent += tmp;
            }
        }

        private void ReadThreadWorker()
        {
            try
            {
                var buffer = new CircularByteBuffer(1024 * 1024);

                while (true)
                {
                    if (Socket == null)
                    {
                        Status = HotaStatus.Disconnected;
                        return;
                    }

                    if (!Socket.Connected)
                    {
                        Status = HotaStatus.Disconnected;
                        return;
                    }

                    int availableData = Socket.Available;
                    if (availableData == 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    var tmpBuffer = new byte[65535];
                    while (Socket.Available > 0)
                    {
                        int read = Socket.Receive(tmpBuffer);
                        if (read > 0)
                        {
                            LastMessageReceivedAt = DateTime.Now;
                            buffer.PushData(tmpBuffer, 0, read);

                            RawLogger.Write(tmpBuffer, read);
                        }
                    }

                    while (TryReadPackage(buffer, out var package))
                    {
                        if (package == null)
                        {
                            continue;
                        }

                        PackageLogger.LogPackage(true, package);

                        ProcessPackage(package);
                    }
                }
            }
            catch (Exception)
            {
                Status = HotaStatus.Disconnected;
            }
        }
        private bool TryReadPackage(CircularByteBuffer buffer, out DataPackage? package)
        {
            bool lengthRead = buffer.TryPeekShort(out var nextPackageLength);
            if (!lengthRead)
            {
                package = null;
                return false;
            }

            if (!buffer.TryRead(nextPackageLength, out var packageContent))
            {
                package = null;
                return false;
            }

            package = new DataPackage(packageContent);
            return true;
        }

        private void ProcessPackage(DataPackage dataPackage)
        {
            switch (dataPackage.Type)
            {
                case 0x7F: //donation goal status
                    EventLogger.LogEvent(dataPackage.Type, "donation goal");
                    return;

                case 0x80: //donators
                    EventLogger.LogEvent(dataPackage.Type, "donator");
                    return;

                case 0x31: //auth reply
                    ProcessAuthenticationReply(dataPackage);
                    return;

                case 0x72: //successfully logged in
                    Status = HotaStatus.Authenticated;
                    EventLogger.LogEvent(dataPackage.Type, "login ok");
                    return;

                case 0x47:
                    //lobby chat message
                    EventLogger.LogEvent(dataPackage.Type, "lobby chat");
                    return;

                default:
                    EventLogger.LogEvent(dataPackage.Type, "unhandled");
                    break;
            }
        }

        private void ProcessAuthenticationReply(DataPackage package)
        {
            var reply = package.ReadInt(4);

            switch (reply)
            {
                case 1:
                    EventLogger.LogEvent(package.Type, "login ok");
                    StartPingThread();
                    return;

                case 2:
                    EventLogger.LogEvent(package.Type, "login not ok", "Already logged in.");
                    Status = HotaStatus.Disconnected;
                    return;

                default:
                    EventLogger.LogEvent(package.Type, "login not ok", $"Minimum required version: {reply}");
                    Status = HotaStatus.ObsoleteClient;
                    return;
            }
        }

        #region IDisposable implementation
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SafeAbort(PingThread);
                    PingThread = null;

                    SafeAbort(ReadThread);
                    ReadThread = null;

                    SafeDispose(Socket);
                    Socket = null;

                    SafeDispose(RawLogger);
                    RawLogger = HotaRawLogger.Null;

                    SafeDispose(PackageLogger);
                    PackageLogger = HotaPackageLogger.Null;

                    SafeDispose(EventLogger);
                    EventLogger = HotaEventLogger.Null;
                }

                disposedValue = true;
            }
        }

        private static void SafeDispose(IDisposable? disposable)
        {
            try
            {
                disposable?.Dispose();
            }
            catch { }
        }

        private static void SafeAbort(Thread? thread)
        {
            try
            {
                thread?.Interrupt();
            }
            catch { }
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
