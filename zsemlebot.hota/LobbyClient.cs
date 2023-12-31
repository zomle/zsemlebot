﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using zsemlebot.core;
using zsemlebot.core.Domain;
using zsemlebot.core.Enums;
using zsemlebot.core.EventArgs;
using zsemlebot.core.Extensions;
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

        private EventHandler<OwnInfoReceivedArgs>? ownInfoReceived;
        public event EventHandler<OwnInfoReceivedArgs> OwnInfoReceived
        {
            add { ownInfoReceived += value; }
            remove { ownInfoReceived -= value; }
        }

        #endregion

        private HotaClientStatus status;
        public HotaClientStatus Status
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
            Status = HotaClientStatus.Initialized;

            RawLogger = HotaRawLogger.Null;
            EventLogger = HotaEventLogger.Null;
            PackageLogger = HotaPackageLogger.Null;
        }

        public void ReplayBinaryFile(string filePath)
        {
            var thread = new Thread(_ => { ReplayBinaryFileThreadWorker(filePath); });
            thread.Start();
        }

        private void ReplayBinaryFileThreadWorker(string filePath)
        {
            Debug.WriteLine("Replay thread started.");

            var buffer = new CircularByteBuffer(1024 * 1024);
            using var stream = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));

            while (true)
            {
                var bytes = stream.ReadBytes(1024);
                if (bytes.Length == 0)
                {
                    break;
                }

                buffer.PushData(bytes, 0, bytes.Length);

                while (buffer.TryReadPackage(out var package))
                {
                    if (package == null)
                    {
                        continue;
                    }

                    LastMessageReceivedAt = DateTime.Now;
                    ProcessPackage(package);
                }

                Thread.Sleep(1);
            }

            Debug.WriteLine("Replay thread finished.");
        }

        public bool Connect()
        {
            if (Config.Instance.Hota.ServerAddress == null)
            {
                return false;
            }

            var now = DateTime.Now;

            Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            Status = HotaClientStatus.Connecting;

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

                Status = HotaClientStatus.Connected;

                SendLoginMessage();

                lastMessageReceivedAt = DateTime.Now;

                ReadThread = new Thread(ReadThreadWorker);
                ReadThread.Start();

                return true;
            }
            catch (Exception)
            {
                Status = HotaClientStatus.Disconnected;
                return false;
            }
        }

        public void SendMessage(uint userId, string message)
        {
            SendSocketRaw(new SendChatMessage(userId, message));
        }

        public void GetUserElo(uint userId)
        {
            SendSocketRaw(new RequestUserEloMessage(userId));
        }

        public void GetUserRep(uint userId)
        {
            SendSocketRaw(new RequestUserRepMessage(userId));
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
                    bool shouldPingAgain = DateTime.Now - LastPingSentAt > PingFrequency;
                    if (Status == HotaClientStatus.Connected && shouldPingAgain)
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
                Status = HotaClientStatus.Disconnected;
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
                        Status = HotaClientStatus.Disconnected;
                        return;
                    }

                    if (!Socket.Connected)
                    {
                        Status = HotaClientStatus.Disconnected;
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
                Status = HotaClientStatus.Disconnected;
            }
        }

        private bool ProcessPackage(DataPackage dataPackage)
        {
            switch (dataPackage.Type)
            {
                case Constants.MsgTypex31_AuthReply: //auth reply
                    ProcessAuthenticationReply(dataPackage);
                    return true;

                case Constants.MsgTypex33_UserJoinedLobby: //user joined lobby
                    ProcessUserJoinedLobby(dataPackage);
                    return true;

                case Constants.MsgTypex34_OwnInfo: //own info
                    ProcessOwnInfo(dataPackage);
                    return true;

                case Constants.MsgTypex36_UserStatusChange: //user status change
                    ProcessUserStatusChange(dataPackage);
                    return true;

                case Constants.MsgTypex38_GameRoomItem: //game room item
                    ProcessGameRoomItem(dataPackage);
                    return true;

                case Constants.MsgTypex39_GameUserChange: //game-user change
                    ProcessGameUserChange(dataPackage);
                    return true;

                case Constants.MsgTypex3A_GameEnded: //game ended
                    ProcessGameEnded(dataPackage);
                    return true;

                case Constants.MsgTypex46_OldChatMessage: //old chat message
                    EventLogger.LogEvent(dataPackage.Type, "old chat");
                    return true;

                case Constants.MsgTypex47_NewChatMessage: //incoming chat message
                    ProcessIncomingMessage(dataPackage);
                    return true;

                case Constants.MsgTypex53_UserLeftLobby: //user left lobby
                    ProcessUserLeftLobby1(dataPackage);
                    return true;

                case Constants.MsgTypex69_UserRepUpdate:
                    ProcessUserRepUpdate(dataPackage);
                    return true;

                case Constants.MsgTypex6B_UserLeftLobby2: //user left lobby
                    ProcessUserLeftLobby2(dataPackage);
                    return true;

                case Constants.MsgTypex72_SuccessfulLogin: //successfully logged in
                    Status = HotaClientStatus.Authenticated;
                    EventLogger.LogEvent(dataPackage.Type, "login ok");
                    return true;
                
                case Constants.MsgTypex75_UserEloUpdate:
                    ProcessUserEloUpdate(dataPackage);
                    return true;

                case Constants.MsgTypex7E_NewDonation: //new donation received
                    EventLogger.LogEvent(dataPackage.Type, "new donation");
                    return true;

                case Constants.MsgTypex7F_DonationGoal: //donation goal status
                    EventLogger.LogEvent(dataPackage.Type, "donation goal");
                    return true;

                case Constants.MsgTypex80_Donations: //donations
                    EventLogger.LogEvent(dataPackage.Type, "donations");
                    return true;

                case Constants.MsgTypex85_Unknown1:
                    EventLogger.LogEvent(dataPackage.Type, "unknown1");
                    return true;

                case Constants.MsgTypex8A_Unknown2: //maybe end of data - refresh ui?
                    EventLogger.LogEvent(dataPackage.Type, "unknown2");
                    return true;

                case Constants.MsgTypex8C_GameStarted: //game started
                    ProcessGameStarted(dataPackage);
                    return true;

                case Constants.MsgTypex6C_UnknownUserEvent:
                    EventLogger.LogEvent(dataPackage.Type, "unkwn usr evnt", $"user id: {dataPackage.ReadInt(4).ToHexString()}");
                    return true;

                default:
                    EventLogger.LogEvent(dataPackage.Type, "unhandled");
                    return false;
            }
        }

        private void ProcessGameEnded(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgTypex3A_GameEnded);

            var hostUserId = dataPackage.ReadUInt(4);
            var gameId = dataPackage.ReadInt(8);

            EventLogger.LogEvent(dataPackage.Type, "game ended", $"host user id: {hostUserId.ToHexString()}; game id: {gameId.ToHexString()}");

            EnqueueEvent(new GameEnded(new GameKey(hostUserId, gameId)));
        }

        private void ProcessGameStarted(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgTypex8C_GameStarted);

            var hostUserId = dataPackage.ReadInt(4);
            var gameId = dataPackage.ReadInt(8);
            var playerCount = dataPackage.ReadInt(0xc);

            EventLogger.LogEvent(dataPackage.Type, "game started", $"host user id: {hostUserId.ToHexString()}; game id: {gameId.ToHexString()}; player count: {playerCount.ToHexString()}");
        }

        private void ProcessUserStatusChange(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgTypex36_UserStatusChange);

            var userId = dataPackage.ReadUInt(4);
            var newStatus = dataPackage.ReadInt(8);
            EventLogger.LogEvent(dataPackage.Type, "user stus chg", $"user id: {userId.ToHexString()}; new status: {newStatus}");

            EnqueueEvent(new UserStatusChange(userId, (short)newStatus));
        }

        private void ProcessGameUserChange(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgTypex39_GameUserChange);

            var hostUserId = dataPackage.ReadUInt(4);
            var gameId = dataPackage.ReadInt(8);
            var otherUserId = dataPackage.ReadUInt(0xc);
            var joinLeaveRoom1 = dataPackage.ReadByte(0x10);
            var otherGameId = dataPackage.ReadInt(0x11);
            var gameStatusUpdate = dataPackage.ReadByte(0x15);

            EventLogger.LogEvent(dataPackage.Type, "game user chg", $"host user id: {hostUserId.ToHexString()}; game id: {gameId.ToHexString()}; other user id: {otherUserId.ToHexString()}; join/leave: {joinLeaveRoom1.ToHexString()}; other game id: {otherGameId.ToHexString()}; game status update: {gameStatusUpdate.ToHexString()}");

            if (otherUserId == 0)
            {
                return;
            }

            if (joinLeaveRoom1 == 0x01)
            {
                EnqueueEvent(new GameRoomUserJoined(new GameKey(hostUserId, gameId), otherUserId));
            }
            else if (joinLeaveRoom1 == 0xFF)
            {
                EnqueueEvent(new GameRoomUserLeft(new GameKey(hostUserId, gameId), otherUserId));
            }

            if (gameStatusUpdate == 0x01)
            {
                EnqueueEvent(new GameStarted(new GameKey(hostUserId, gameId)));
            }
        }

        private void ProcessOwnInfo(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgTypex34_OwnInfo);

            var userId = dataPackage.ReadUInt(8);
            var userElo = dataPackage.ReadInt(0xC);
            var userRep = dataPackage.ReadInt(0x10);
            var userName = dataPackage.ReadString(0x14, 18);

            EventLogger.LogEvent(dataPackage.Type, "own info", $"user id: {userId.ToHexString()}; name: {userName}; elo: {userElo}; rep: {userRep}");

            ownInfoReceived?.Invoke(this, new OwnInfoReceivedArgs(userName, userId));
        }

        private void ProcessGameRoomItem(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgTypex38_GameRoomItem);

            var hostUserId = dataPackage.ReadUInt(4);
            var gameId = dataPackage.ReadInt(8);
            var gameDescription = dataPackage.ReadString(0xC, 64);
            var isPasswordProtected = dataPackage.ReadByte(0x4C) == 1;
            var maxPlayerCount = dataPackage.ReadByte(0x4D);
            var isRanked = dataPackage.ReadByte(0x4E) == 1;
            var isLoadGame = dataPackage.ReadByte(0x4F) == 1;
            var currentNumberOfPlayers = dataPackage.ReadByte(0x50);

            var joinedUserIds = new List<uint>();
            for (int i = 0; i < currentNumberOfPlayers; i++)
            {
                joinedUserIds.Add(dataPackage.ReadUInt(0x51 + i * 4));
            }
            var hostUserElo = dataPackage.ReadInt(0x76);

            EventLogger.LogEvent(dataPackage.Type, "game created", $"game id: {gameId.ToHexString()}; host user id: {hostUserId.ToHexString()} ({hostUserElo}); Joined users: {string.Join(", ", joinedUserIds.Select(u => u.ToHexString()))}");

            EnqueueEvent(new GameRoomCreated(new GameKey(hostUserId, gameId), gameDescription, isRanked, isLoadGame, maxPlayerCount, joinedUserIds));
        }

        private void ProcessUserJoinedLobby(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgTypex33_UserJoinedLobby);

            var status = dataPackage.ReadShort(6);
            var userId = dataPackage.ReadUInt(8);
            var userElo = dataPackage.ReadInt(0xC);
            var userRep = dataPackage.ReadInt(0x10);
            var userName = dataPackage.ReadString(0x14, 18);

            EventLogger.LogEvent(dataPackage.Type, "user join lby", $"{userName:-18}({userId.ToHexString()}) elo: {userElo}; rep: {userRep}; status: {status}");

            EnqueueEvent(new UserJoinedLobby(userId, userName, userElo, userRep, status));
        }

        private void ProcessUserEloUpdate(DataPackage dataPackage)
        {
            var userId = dataPackage.ReadUInt(4);
            var elo = dataPackage.ReadInt(8);

            EventLogger.LogEvent(dataPackage.Type, "user elo", $"user id: {userId.ToHexString()}; elo: {elo}");

            EnqueueEvent(new UserEloUpdate(userId, elo));
        }

        private void ProcessUserRepUpdate(DataPackage dataPackage)
        {
            var userId = dataPackage.ReadUInt(4);
            var friendLists = dataPackage.ReadShort(8);
            var blackLists = dataPackage.ReadShort(10);

            EventLogger.LogEvent(dataPackage.Type, "user rep", $"user id: {userId.ToHexString()}; friendlists: {friendLists}; blacklists: {blackLists}");

            EnqueueEvent(new UserRepUpdate(userId, friendLists, blackLists));
        }

        private void ProcessUserLeftLobby1(DataPackage dataPackage)
        {
            var userId = dataPackage.ReadUInt(4);

            EventLogger.LogEvent(dataPackage.Type, "user left", $"user id: {userId.ToHexString()}");

            EnqueueEvent(new UserLeftLobby(userId));
        }
        private void ProcessUserLeftLobby2(DataPackage dataPackage)
        {
            var userId = dataPackage.ReadUInt(4);

            EventLogger.LogEvent(dataPackage.Type, "user left2", $"user id: {userId.ToHexString()}");

            EnqueueEvent(new UserLeftLobby(userId));
        }

        private void ProcessIncomingMessage(DataPackage dataPackage)
        {
            AssertType(dataPackage, Constants.MsgTypex47_NewChatMessage);

            var sourceUserId = dataPackage.ReadUInt(4);
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
            AssertType(dataPackage, Constants.MsgTypex31_AuthReply);

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
                    Status = HotaClientStatus.Disconnected;
                    return;

                default:
                    EventLogger.LogEvent(dataPackage.Type, "login not ok", $"Minimum required version: {reply}");
                    MinimumClientVersion = reply;
                    Status = HotaClientStatus.ObsoleteClient;
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
