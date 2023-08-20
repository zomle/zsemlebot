using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using zsemlebot.core;
using zsemlebot.core.Enums;
using zsemlebot.core.EventArgs;
using zsemlebot.hota.Events;
using zsemlebot.hota.Extensions;
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
                    statusChanged?.Invoke(this, new HotaStatusChangedArgs(value, MinimumClientVersion));
                }
            }
        }

        private int? MinimumClientVersion { get; set; }

        private Socket? Socket { get; set; }

        private Thread? ReadThread { get; set; }
        private Thread? PingThread { get; set; }

        private Queue<HotaEvent> IncomingEventQueue { get; set; }
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
        private static readonly object padlock = new object();

        public LobbyClient()
        {
            IncomingEventQueue = new Queue<HotaEvent>();
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

                lock (padlock)
                {
                    IncomingEventQueue.Clear();
                }

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

        public void SendMessage(uint userId, string message)
        {
            SendSocketRaw(new SendChatMessage(userId, message));
        }

        public bool HasNewEvent()
        {
            lock (padlock)
            {
                return IncomingEventQueue.Count > 0;
            }
        }

        public HotaEvent? GetNextEvent()
        {
            lock (padlock)
            {
                IncomingEventQueue.TryDequeue(out var result);
                return result;
            }
        }

        private void EnqueueEvent(HotaEvent newEvent)
        {
            lock (padlock)
            {
                IncomingEventQueue.Enqueue(newEvent);
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
            PackageLogger.LogPackage(false, package, true);

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

                    while (buffer.TryReadPackage(out var package))
                    {
                        if (package == null)
                        {
                            continue;
                        }

                        var isHandled = ProcessPackage(package);
                        PackageLogger.LogPackage(true, package, isHandled);
                    }
                }
            }
            catch (Exception)
            {
                Status = HotaStatus.Disconnected;
            }
        }

        private bool ProcessPackage(DataPackage dataPackage)
        {
            switch (dataPackage.Type)
            {
                case Constants.MsgType_AuthReply: //auth reply
                    ProcessAuthenticationReply(dataPackage);
                    return true;

                case Constants.MsgType_UserJoinedLobby: //user joined lobby
                    ProcessUserJoinedLobby(dataPackage);
                    return true;

                case Constants.MsgType_OwnInfo: //own info
                    ProcessOwnInfo(dataPackage);
                    return true;

                case Constants.MsgType_UserStatusChange: //user status change
                    ProcessUserStatusChange(dataPackage);
                    return true;

                case Constants.MsgType_GameRoomItem: //game room item
                    ProcessGameRoomItem(dataPackage);
                    return true;

                case Constants.MsgType_GameUserChange: //game-user change
                    ProcessGameUserChange(dataPackage);
                    return true;

                case Constants.MsgType_GameEnded: //game ended
                    ProcessGameEnded(dataPackage);
                    return true;

                case Constants.MsgType_OldChatMessage: //old chat message
                    EventLogger.LogEvent(dataPackage.Type, "old chat");
                    return true;

                case Constants.MsgType_NewChatMessage: //incoming chat message
                    ProcessIncomingMessage(dataPackage);
                    return true;

                case Constants.MsgType_UserLeftLobby: //user left lobby
                    ProcessUserLeftLobby1(dataPackage);
                    return true;

                case Constants.MsgType_UserLeftLobby2: //user left lobby
                    ProcessUserLeftLobby2(dataPackage);
                    return true;

                case Constants.MsgType_SuccessfulLogin: //successfully logged in
                    Status = HotaStatus.Authenticated;
                    EventLogger.LogEvent(dataPackage.Type, "login ok");
                    return true;

                case Constants.MsgType_DonationGoal: //donation goal status
                    EventLogger.LogEvent(dataPackage.Type, "donation goal");
                    return true;

                case Constants.MsgType_Donators: //donators
                    EventLogger.LogEvent(dataPackage.Type, "donator");
                    return true;

                case Constants.MsgType_Unknown1:
                    EventLogger.LogEvent(dataPackage.Type, "unknown1");
                    return true;

                case Constants.MsgType_Unknown2: //maybe end of data - refresh ui?
                    EventLogger.LogEvent(dataPackage.Type, "unknown2");
                    return true;

                case Constants.MsgType_GameStatusChange: //maybe game status change
                    ProcessGameStatusChange(dataPackage);
                    return true;

                case Constants.MsgType_UnknownUserEvent:
                    EventLogger.LogEvent(dataPackage.Type, "unkwn usr evnt", $"user id: {dataPackage.ReadInt(4).ToHexString()}");
                    return true;

                default:
                    EventLogger.LogEvent(dataPackage.Type, "unhandled");
                    return false;
            }
        }

        private void ProcessGameEnded(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgType_GameEnded);

            var hostUserId = dataPackage.ReadInt(4);
            var gameId = dataPackage.ReadInt(8);

            EventLogger.LogEvent(dataPackage.Type, "game ended", $"host user id: {hostUserId.ToHexString()}; game id: {gameId.ToHexString()}");
        }

        private void ProcessGameStatusChange(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgType_GameStatusChange);

            var hostUserId = dataPackage.ReadInt(4);
            var gameId = dataPackage.ReadInt(8);
            var field = dataPackage.ReadInt(0xc);

            EventLogger.LogEvent(dataPackage.Type, "game stus chg", $"host user id: {hostUserId.ToHexString()}; game id: {gameId.ToHexString()}; field: {field.ToHexString()}");
        }

        private void ProcessUserStatusChange(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgType_UserStatusChange);

            var userId = dataPackage.ReadInt(4);
            var newStatus = dataPackage.ReadInt(8);
            EventLogger.LogEvent(dataPackage.Type, "user stus chg", $"user id: {userId.ToHexString()}; new status: {newStatus}");
        }

        private void ProcessGameUserChange(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgType_GameUserChange);

            var hostUserId = dataPackage.ReadInt(4);
            var gameId = dataPackage.ReadInt(8);
            var otherUserId = dataPackage.ReadInt(0xc);
            var field1 = dataPackage.ReadByte(0x10);
            var unkwnid = dataPackage.ReadInt(0x11);
            var field2 = dataPackage.ReadByte(0x15);

            EventLogger.LogEvent(dataPackage.Type, "game user chg", $"host user id: {hostUserId.ToHexString()}; game id: {gameId.ToHexString()}; other user id: {otherUserId.ToHexString()}; field1: {field1.ToHexString()}; unknownid: {unkwnid.ToHexString()}; field2: {field2.ToHexString()}");
        }

        private void ProcessOwnInfo(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgType_OwnInfo);

            var userId = dataPackage.ReadInt(8);
            var userElo = dataPackage.ReadInt(0xC);
            var userRep = dataPackage.ReadInt(0x10);
            var userName = dataPackage.ReadString(0x14, 18);

            EventLogger.LogEvent(dataPackage.Type, "own info", $"user id: {userId.ToHexString()}; name: {userName}; elo: {userElo}; rep: {userRep}");
        }

        private void ProcessGameRoomItem(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgType_GameRoomItem);

            var hostUserId = dataPackage.ReadInt(4);
            var gameId = dataPackage.ReadInt(8);
            var gameDescription = dataPackage.ReadString(0xC, 64);
            var isPasswordProtected = dataPackage.ReadByte(0x4C) == 1; //???
            var maxNumberOfPlayers = dataPackage.ReadByte(0x4D);
            var isRanked = dataPackage.ReadByte(0x4E) == 1;
            var isLoadGame = dataPackage.ReadByte(0x4F) == 1; //????
            var currentNumberOfPlayers = dataPackage.ReadByte(0x50);

            var joinedUserIds = new List<int>();
            for (int i = 0; i < currentNumberOfPlayers; i++)
            {
                joinedUserIds.Add(dataPackage.ReadInt(0x51 + i * 4));
            }
            var hostUserElo = dataPackage.ReadInt(0x76);

            EventLogger.LogEvent(dataPackage.Type, "game created", $"game id: {gameId.ToHexString()}; host user id: {hostUserId.ToHexString()} ({hostUserElo}); Joined users: {string.Join(", ", joinedUserIds.Select(u => u.ToHexString()))}");
        }

        private void ProcessUserJoinedLobby(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgType_UserJoinedLobby);

            var userId = dataPackage.ReadInt(8);
            var userElo = dataPackage.ReadInt(0xC);
            var userRep = dataPackage.ReadInt(0x10);
            var userName = dataPackage.ReadString(0x14, 18);

            EventLogger.LogEvent(dataPackage.Type, "user join lby", $"{userName:-18}({userId.ToHexString()}) elo: {userElo}; rep: {userRep}");

            EnqueueEvent(new UserJoinedLobby(userId, userName, userElo, userRep));
        }

        private void ProcessUserLeftLobby1(DataPackage dataPackage)
        {
            var userId = dataPackage.ReadInt(4);
            
            EventLogger.LogEvent(dataPackage.Type, "user left", $"user id: {userId.ToHexString()}");

            EnqueueEvent(new UserLeftLobby(userId));
        }
        private void ProcessUserLeftLobby2(DataPackage dataPackage)
        {
            var userId = dataPackage.ReadInt(4);
            
            EventLogger.LogEvent(dataPackage.Type, "user left2", $"user id: {userId.ToHexString()}");

            EnqueueEvent(new UserLeftLobby(userId));
        }

        private void ProcessIncomingMessage(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgType_NewChatMessage);

            var sourceUserId = dataPackage.ReadInt(4);
            var destinationId = dataPackage.ReadInt(8);
            var destinationType = dataPackage.ReadByte(0xC);
            var sourceUserName = dataPackage.ReadString(0x22, 17);
            var message = dataPackage.ReadString(0x33, dataPackage.Content.Length - 0x33);

            string destination = destinationType switch
            {
                Constants.ChatMessageDstType_PublicLobby => ((ChatMessageDestination)destinationId).ToString(),
                Constants.ChatMessageDstType_PrivateMessage => destinationId.ToHexString(),
                _ => $"unknown:{destinationType}"
            };

            EventLogger.LogEvent(dataPackage.Type, "incoming chat", $"{sourceUserName:-18}({sourceUserId.ToHexString()}) -> ({destination}): {message}");

            if (destinationType == Constants.ChatMessageDstType_PrivateMessage)
            {
                EnqueueEvent(new IncomingMessage(sourceUserId, message));
            }
        }

        private void ProcessAuthenticationReply(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgType_AuthReply);

            var reply = dataPackage.ReadInt(4);

            switch (reply)
            {
                case 1:
                    EventLogger.LogEvent(dataPackage.Type, "login ok");
                    MinimumClientVersion = null;
                    StartPingThread();
                    return;

                case 2:
                    EventLogger.LogEvent(dataPackage.Type, "login not ok", "Already logged in.");
                    MinimumClientVersion = null;
                    Status = HotaStatus.Disconnected;
                    return;

                default:
                    EventLogger.LogEvent(dataPackage.Type, "login not ok", $"Minimum required version: {reply}");
                    MinimumClientVersion = reply;
                    Status = HotaStatus.ObsoleteClient;
                    return;
            }
        }

        private static void AssertType(DataPackage package, short expectedType, [CallerMemberName] string? callerName = null)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package), $"{callerName} was called with null package.");
            }

            if (package.Type != expectedType)
            {
                throw new InvalidOperationException($"{callerName}(): Expected package type: {expectedType}; Actual package type: {package.Type}");
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
