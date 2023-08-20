using System;
using System.Collections.Generic;
using System.Linq;
using zsemlebot.repository.Models;

namespace zsemlebot.repository
{
    public class BotRepository : RepositoryBase
    {
        public static readonly BotRepository Instance;

        private Dictionary<int, List<TwitchHotaLink>> LinksByHotaUserId { get; set; }
        private Dictionary<int, List<TwitchHotaLink>> LinksByTwitchUserId { get; set; }
        private List<TwitchHotaLinkRequest> UserLinkRequests { get; set; }

        static BotRepository()
        {
            Instance = new BotRepository();
        }

        private BotRepository()
        {
            LinksByHotaUserId = new Dictionary<int, List<TwitchHotaLink>>();
            LinksByTwitchUserId = new Dictionary<int, List<TwitchHotaLink>>();

            UserLinkRequests = new List<TwitchHotaLinkRequest>();

            LoadTwitchHotaUserLinks();
            LoadTwitchHotaUserLinkRequests();
        }

        public void CreateUserLinkRequest(int hotaUserId, string twitchUserName, string authCode, int validityLengthInMins)
        {
            //delete potentially existing request
            DeleteUserLinkRequest(hotaUserId, twitchUserName);

            var request = new TwitchHotaLinkRequest { TwitchUserName = twitchUserName, HotaUserId = hotaUserId, AuthCode = authCode, ValidUntilUtc = DateTime.UtcNow + TimeSpan.FromMinutes(validityLengthInMins) };
            UserLinkRequests.Add(request);

            EnqueueWorkItem(@$"INSERT INTO [{TwitchHotaUserLinkRequestTableName}] ([TwitchUserName], [HotaUserId], [AuthCode], [ValidUntilUtc]) 
                           VALUES (@twitchUserName, @hotaUserId, @authCode, datetime('now', '+{validityLengthInMins} minutes'));", new { twitchUserName, hotaUserId, authCode });
        }

        public core.Domain.TwitchHotaLinkRequest? GetUserLinkRequest(int hotaUserId, string twitchUserName)
        {
            var result = UserLinkRequests
                .Where(r => r.HotaUserId == hotaUserId && string.Equals(r.TwitchUserName, twitchUserName, StringComparison.InvariantCultureIgnoreCase))
                .Select(r => new core.Domain.TwitchHotaLinkRequest { HotaUserId = r.HotaUserId, TwitchUserName = r.TwitchUserName, AuthCode = r.AuthCode, ValidUntilUtc = r.ValidUntilUtc })
                .FirstOrDefault();
            return result;
        }

        public IReadOnlyList<core.Domain.TwitchHotaLinkRequest> ListUserLinkRequests(string twitchUserName)
        {
            var result = UserLinkRequests
                .Where(r => string.Equals(r.TwitchUserName, twitchUserName, StringComparison.InvariantCultureIgnoreCase))
                .Select(r => new core.Domain.TwitchHotaLinkRequest { HotaUserId = r.HotaUserId, TwitchUserName = r.TwitchUserName, AuthCode = r.AuthCode, ValidUntilUtc = r.ValidUntilUtc })
                .ToList();
            return result;
        }

        public void DeleteUserLinkRequest(int hotaUserId, string twitchUserName)
        {
            UserLinkRequests.RemoveAll(r => r.HotaUserId == hotaUserId && string.Equals(r.TwitchUserName, twitchUserName, StringComparison.InvariantCultureIgnoreCase));
            EnqueueWorkItem(@$"DELETE FROM [{TwitchHotaUserLinkRequestTableName}]
                           WHERE [TwitchUserName] = @twitchUserName AND [HotaUserId] = @hotaUserId", new { twitchUserName, hotaUserId });
        }

        public void UpdateUserLinkRequest(core.Domain.TwitchHotaLinkRequest request, int validityLengthInMins)
        {
            request.ValidUntilUtc = DateTime.UtcNow + TimeSpan.FromMinutes(validityLengthInMins);
            EnqueueWorkItem(@$"UPDATE [{TwitchHotaUserLinkRequestTableName}]
                                SET [ValidUntilUtc] = datetime('now', '+{validityLengthInMins} minutes')
                                WHERE [TwitchUserName] = @twitchUserName AND [HotaUserId] = @hotaUserId",
                                new { twitchUserName = request.TwitchUserName, hotaUserId = request.HotaUserId });
        }

        private void LoadTwitchHotaUserLinkRequests()
        {
            var models = Query<TwitchHotaLinkRequest>($"SELECT [TwitchUserName], [HotaUserId], [AuthCode], [ValidUntilUtc] FROM [{TwitchHotaUserLinkRequestTableName}];");
            foreach (var model in models)
            {
                UserLinkRequests.Add(model);
            }
        }

        public void AddTwitchHotaUserLink(int twitchUserId, int hotaUserId)
        {
            var newLink = new TwitchHotaLink { TwitchUserId = twitchUserId, HotaUserId = hotaUserId };
            AddTwitchHotaUserLink(newLink);

            EnqueueWorkItem(@$"INSERT INTO [{TwitchHotaUserLinkTableName}] ([TwitchUserId], [HotaUserId], [CreatedAtUtc]) 
                           VALUES (@twitchUserId, @hotaUserId, datetime('now'));", new { twitchUserId, hotaUserId });
        }

        public void DelTwitchHotaUserLink(int twitchUserId, int hotaUserId)
        {
            if (LinksByHotaUserId.TryGetValue(hotaUserId, out var lst))
            {
                lst.RemoveAll(l => l.TwitchUserId == twitchUserId && l.HotaUserId == hotaUserId);
            }

            if (LinksByTwitchUserId.TryGetValue(twitchUserId, out lst))
            {
                lst.RemoveAll(l => l.TwitchUserId == twitchUserId && l.HotaUserId == hotaUserId);
            }

            EnqueueWorkItem(@$"DELETE FROM [{TwitchHotaUserLinkTableName}]
                                WHERE [TwitchUserId] = @twitchUserId AND [HotaUserId] = @hotaUserId;", new { twitchUserId, hotaUserId });
        }

        private void LoadTwitchHotaUserLinks()
        {
            var models = Query<TwitchHotaLink>($"SELECT [TwitchUserId], [HotaUserId] FROM [{TwitchHotaUserLinkTableName}];");
            foreach (var model in models)
            {
                AddTwitchHotaUserLink(model);
            }
        }

        private void AddTwitchHotaUserLink(TwitchHotaLink link)
        {
            if (!LinksByHotaUserId.TryGetValue(link.HotaUserId, out var lst))
            {
                lst = new List<TwitchHotaLink>();
                LinksByHotaUserId.Add(link.HotaUserId, lst);
            }
            lst.Add(link);

            if (!LinksByTwitchUserId.TryGetValue(link.TwitchUserId, out lst))
            {
                lst = new List<TwitchHotaLink>();
                LinksByTwitchUserId.Add(link.TwitchUserId, lst);
            }
            lst.Add(link);
        }
    }
}
