using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using zsemlebot.core;
using zsemlebot.core.Domain;
using zsemlebot.core.Enums;
using zsemlebot.core.EventArgs;
using zsemlebot.core.Extensions;
using zsemlebot.hota;
using zsemlebot.hota.Events;
using zsemlebot.repository;
using zsemlebot.services.Log;

namespace zsemlebot.services
{
    public class UserUpdateResponse
    {
        public List<HotaUser> UpdatedUsers { get; set; }
        public List<HotaUser> NotUpdatedUsers { get; set; }

        public UserUpdateResponse(IEnumerable<HotaUser> updatedUsers, IEnumerable<HotaUser> notUpdatedUsers)
        {
            UpdatedUsers = new List<HotaUser>(updatedUsers);
            NotUpdatedUsers = new List<HotaUser>(notUpdatedUsers);
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

        private EventHandler<HotaGameListChangedArgs>? gameListChanged;
        public event EventHandler<HotaGameListChangedArgs> GameListChanged
        {
            add { gameListChanged += value; }
            remove { gameListChanged -= value; }
        }
        #endregion

        private Dictionary<uint, HotaUser> OnlineUsers { get; }
        private HotaGameDirectory GameDirectory { get; }
        private LobbyClient? Client { get; set; }

        private string? OwnUserName { get; set; }
        private uint OwnUserId { get; set; }

        private Thread HandleMessagesThread { get; set; }
        private int ReconnectCount { get; set; }

        private bool PauseUpdateNotifications { get; set; }

        private static HotaRepository HotaRepository { get { return HotaRepository.Instance; } }
        private static BotRepository BotRepository { get { return BotRepository.Instance; } }

        private static readonly int[] WaitTimesBetweenReconnect = { 2, 5, 10, 15, 30 };

        public HotaService()
        {
            ReconnectCount = 0;
            OnlineUsers = new Dictionary<uint, HotaUser>(5000);
            GameDirectory = new HotaGameDirectory();

            HandleMessagesThread = new Thread(HandleMessagesWorker);
            HandleMessagesThread.Start();
        }
        public void Test()
        {
            Client = new LobbyClient();
            AddEventHandlers(Client);

            Client.ReplayBinaryFile(@"c:\projects\traffic_20230805_023554.bin");
        }

        public bool Connect()
        {
            if (Client != null)
            {
                return true;
            }

            Client = new LobbyClient();
            AddEventHandlers(Client);

            var connected = Client.Connect();
            if (!connected)
            {
                Client.Dispose();
                Client = null;

                statusChanged?.Invoke(this, new HotaStatusChangedArgs(HotaClientStatus.Initialized));
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
                    if (Client == null || Client.Status == HotaClientStatus.Initialized)
                    {
                        Thread.Sleep(750);
                        continue;
                    }
                    else if (Client.Status == HotaClientStatus.Disconnected)
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
                    else if (Client.Status == HotaClientStatus.Connecting)
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

                    Thread.Sleep(100);
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
            AddEventHandlers(tmpClient);

            var reconnected = tmpClient.Connect();
            if (reconnected)
            {
                RemoveEventHandlers(Client);
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

        public UserUpdateResponse RequestUserEloAndWait(IReadOnlyList<HotaUser> hotaUsers)
        {
            if (Client?.Status != HotaClientStatus.Authenticated)
            {
                return new UserUpdateResponse(Array.Empty<HotaUser>(), hotaUsers);
            }

            var requestTime = DateTime.UtcNow;
            foreach (var hotaUser in hotaUsers)
            {
                Client.GetUserElo(hotaUser.HotaUserId);
            }

            var remainingUsers = new List<HotaUser>(hotaUsers);
            var updatedUsers = new List<HotaUser>();
			var waitUntil = DateTime.UtcNow + Constants.RequestEloTimeOut;
			while (DateTime.UtcNow < waitUntil)
            {
                for (int i = 0; i < remainingUsers.Count;)
                {
                    var user = remainingUsers[i];
                    var tmpUser = HotaRepository.GetUser(user.HotaUserId);
                    if (tmpUser.UpdatedAtUtc > requestTime)
                    {
                        updatedUsers.Add(tmpUser);
                        remainingUsers.RemoveAt(i);
                        continue;
                    }
                    i++;
                }

                if (remainingUsers.Count == 0)
                {
                    break;
                }

				Thread.Sleep(150);
            }

            return new UserUpdateResponse(updatedUsers, remainingUsers);
        }

        public UserUpdateResponse RequestUserRepAndWait(IReadOnlyList<HotaUser> hotaUsers)
        {
            if (Client?.Status != HotaClientStatus.Authenticated)
            {
                return new UserUpdateResponse(Array.Empty<HotaUser>(), hotaUsers);
            }

            var requestTime = DateTime.UtcNow;
            foreach (var hotaUser in hotaUsers)
            {
                Client.GetUserRep(hotaUser.HotaUserId);
            }

            var remainingUsers = new List<HotaUser>(hotaUsers);
            var updatedUsers = new List<HotaUser>();
			var waitUntil = DateTime.UtcNow + Constants.RequestEloTimeOut;
			while (DateTime.UtcNow < waitUntil)
			{
                for (int i = 0; i < remainingUsers.Count;)
                {
                    var user = remainingUsers[i];
                    var tmpUser = HotaRepository.GetUser(user.HotaUserId);
                    if (tmpUser.UpdatedAtUtc > requestTime)
                    {
                        updatedUsers.Add(tmpUser);
                        remainingUsers.RemoveAt(i);
                        continue;
                    }
                    i++;
                }

                if (remainingUsers.Count == 0)
                {
                    break;
                }

				Thread.Sleep(150);
			}

            return new UserUpdateResponse(updatedUsers, remainingUsers);
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

                case UserStatusChange usc:
                    HandleUserStatusChange(usc);
                    break;

                case GameRoomCreated gcr:
                    HandleGameRoomCreated(gcr);
                    break;

                case GameRoomUserJoined guj:
                    HandleGameRoomUserJoined(guj);
                    break;

                case GameRoomUserLeft gul:
                    HandleGameRoomUserLeft(gul);
                    break;

                case GameStarted gs:
                    HandleGameStarted(gs);
                    break;

                case GameEnded ge:
                    HandleGameEnded(ge);
                    break;

                case IncomingMessage im:
                    HandleIncomingMessage(im);
                    break;

                case UserEloUpdate ueu:
                    HandleUserEloUpdate(ueu);
                    break;

                case UserRepUpdate uru:
                    HandleUserRepUpdate(uru);
                    break;
            }
        }

        private void HandleGameRoomCreated(GameRoomCreated evnt)
        {
            var hostUser = GetHotaUser(evnt.GameKey.HostUserId);
            var newGame = new HotaGame(evnt.GameKey, hostUser, evnt.Description)
            {
                IsRanked = evnt.IsRanked,
                IsLoaded = evnt.IsLoadGame,
                MaxPlayerCount = evnt.MaxPlayerCount,
            };

            HotaGameStatus status = HotaGameStatus.RoomCreated;
            foreach (var playerId in evnt.PlayerIds)
            {
                var player = GetHotaUser(playerId);
                newGame.JoinedUsers.Add(player);

                if (player.Status == HotaUserStatus.InGame)
                {
                    status = HotaGameStatus.InProgress;
                }
            }

            newGame.Status = status;

            GameDirectory.AddGame(newGame);

            BotLogger.Instance.LogEvent(BotLogSource.Hota, $"Game created ({newGame.Status}). Host: {hostUser.DisplayName}. Players: {string.Join(", ", newGame.JoinedUsers.Select(ju => ju.DisplayName))}");
        }

        private void HandleGameRoomUserJoined(GameRoomUserJoined evnt)
        {
            var joinedUser = GetHotaUser(evnt.OtherUserId);

            GameDirectory.UserJoin(evnt.GameKey, joinedUser);
        }

        private void HandleGameRoomUserLeft(GameRoomUserLeft evnt)
        {
            var joinedUser = GetHotaUser(evnt.OtherUserId);

            GameDirectory.UserLeft(evnt.GameKey, joinedUser);
        }

        private void HandleGameStarted(GameStarted evnt)
        {
            var game = GameDirectory.GameStarted(evnt.GameKey);

            BotLogger.Instance.LogEvent(BotLogSource.Hota, $"Game started. Players: {string.Join(", ", game.JoinedUsers.Select(ju => ju.DisplayName))}");
        }

        private void HandleGameEnded(GameEnded evnt)
        {
            var game = GameDirectory.GameEnded(evnt.GameKey);

            InvokeGameListChangedEvent();

            BotLogger.Instance.LogEvent(BotLogSource.Hota, $"Game ended. Players: {string.Join(", ", game.JoinedUsers.Select(ju => ju.DisplayName))}");
        }

        private void HandleUserJoinedLobby(UserJoinedLobby evnt)
        {
            var hotaUser = new HotaUser(evnt.HotaUserId, evnt.UserName, evnt.Elo, evnt.Rep, (HotaUserStatus)evnt.Status, DateTime.UtcNow);

            OnlineUsers[evnt.HotaUserId] = hotaUser;
            HotaRepository.UpdateHotaUser(hotaUser);

            InvokeUserListChangedEvent();

            BotLogger.Instance.LogEvent(BotLogSource.Hota, $"User joined. Name: {hotaUser.DisplayName}. Status: {hotaUser.Status}");
        }

        private void HandleUserStatusChange(UserStatusChange evnt)
        {
            var user = GetHotaUser(evnt.HotaUserId, true);

            user.Status = (HotaUserStatus)evnt.NewStatus;
        }

        private void HandleUserLeftLobby(UserLeftLobby evnt)
        {
            var user = GetHotaUser(evnt.HotaUserId, true);
            OnlineUsers.Remove(evnt.HotaUserId);

            InvokeUserListChangedEvent();

            BotLogger.Instance.LogEvent(BotLogSource.Hota, $"User left. Name: {user.DisplayName}.");
        }

        private void HandleIncomingMessage(IncomingMessage evnt)
        {
            if (evnt.SourceUserId == OwnUserId)
            {
                return;
            }

            if (!evnt.Message.StartsWith('!'))
            {
                return;
            }

            var tokens = evnt.Message.Split(' ', 2);
            HandleCommand(evnt.SourceUserId, tokens[0], tokens.Length > 1 ? tokens[1] : null);
        }

        private void HandleUserEloUpdate(UserEloUpdate evnt)
        {
            HotaRepository.UpdateElo(evnt.HotaUserId, evnt.Elo);
        }

        private void HandleUserRepUpdate(UserRepUpdate evnt)
        {
            HotaRepository.UpdateRep(evnt.HotaUserId, evnt.FriendLists);
        }

        private void HandleCommand(uint sourceUserId, string command, string? parameters)
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
            SendChatMessage(source.HotaUserId, MessageTemplates.UserLinkTwitchMessage(authCode, twitchUserName, Config.Instance.Twitch.AdminChannel));
        }

        private void InvokeUserListChangedEvent()
        {
            if (PauseUpdateNotifications)
            {
                return;
            }

            userListChanged?.Invoke(this, new HotaUserListChangedArgs(OnlineUsers.Count));
        }

        private void InvokeGameListChangedEvent()
        {
            if (PauseUpdateNotifications)
            {
                return;
            }

            gameListChanged?.Invoke(this, new HotaGameListChangedArgs(GameDirectory.NotFullCount, GameDirectory.NotStartedCount, GameDirectory.InProgressCount));
        }

        private HotaUser GetHotaUser(uint hotaUserId, bool onlineOnly = false)
        {
            if (!OnlineUsers.TryGetValue(hotaUserId, out var user))
            {
                if (!onlineOnly)
                {
                    user = HotaRepository.GetUser(hotaUserId);
                }

                user ??= new FakeHotaUser(hotaUserId);
            }

            return user;
        }

        private void Client_MessageReceived(object? sender, MessageReceivedArgs e)
        {
            messageReceived?.Invoke(sender, e);
        }

        private void Client_OwnInfoReceived(object? sender, OwnInfoReceivedArgs e)
        {
            OwnUserId = e.UserId;
            OwnUserName = e.DisplayName;

            if (PauseUpdateNotifications)
            {
                PauseUpdateNotifications = false;

            }
            BotLogger.Instance.LogEvent(BotLogSource.Hota, $"Logged in as {OwnUserName}. User id: {OwnUserId.ToHexString()}");
        }

        private void Client_StatusChanged(object? sender, HotaStatusChangedArgs e)
        {
            if (e.NewStatus == HotaClientStatus.Authenticated)
            {
                PauseUpdateNotifications = true;
            }
            statusChanged?.Invoke(sender, e);
        }

        private void AddEventHandlers(LobbyClient client)
        {
            client.StatusChanged += Client_StatusChanged;
            client.MessageReceived += Client_MessageReceived;
            client.OwnInfoReceived += Client_OwnInfoReceived;
        }

        private void RemoveEventHandlers(LobbyClient client)
        {
            client.StatusChanged -= Client_StatusChanged;
            client.MessageReceived -= Client_MessageReceived;
            client.OwnInfoReceived -= Client_OwnInfoReceived;
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
                            RemoveEventHandlers(Client);
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