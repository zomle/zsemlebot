using System;
using System.Collections.Generic;
using zsemlebot.core.Domain;
using zsemlebot.repository.Models;

namespace zsemlebot.repository
{
    public class HotaRepository : RepositoryBase
    {
        public static readonly HotaRepository Instance;

        private Dictionary<int, HotaUserData> HotaUsersById { get; set; }
        private Dictionary<string, HotaUserData> HotaUsersByName { get; set; }

        static HotaRepository()
        {
            Instance = new HotaRepository();
        }

        private HotaRepository()
        {
            HotaUsersById = new Dictionary<int, HotaUserData>();
            HotaUsersByName = new Dictionary<string, HotaUserData>();

            LoadHotaUsers();
        }

        private void LoadHotaUsers()
        {
            var models = Query<HotaUserData>($"SELECT [HotaUserId], [DisplayName], [Elo], [Rep] FROM [{HotaUserDataTableName}];");
            foreach (var model in models)
            {
                HotaUsersById.Add(model.HotaUserId, model);
                HotaUsersByName.Add(model.DisplayName.ToLower(), model);
            }
        }

        public void UpdateHotaUser(HotaUser hotaUser)
        {

            if (HotaUsersById.TryGetValue(hotaUser.HotaUserId, out var oldUser))
            {
                if (oldUser.DisplayName == hotaUser.DisplayName && oldUser.Elo == hotaUser.Elo && oldUser.Rep == hotaUser.Rep)
                {
                    return;
                }

                if (oldUser.DisplayName != hotaUser.DisplayName)
                {
                    var model = new HotaUserData(hotaUser);
                    HotaUsersById[hotaUser.HotaUserId] = model;
                    HotaUsersByName.Remove(oldUser.DisplayName.ToLower());
                    HotaUsersByName.Add(hotaUser.DisplayName.ToLower(), model);
                }

                EnqueueWorkItem(@$"UPDATE [{HotaUserDataTableName}] 
                            SET [DisplayName] = @newName, [Elo] = @elo, [Rep] = @rep, [LastUpdatedAtUtc] = datetime('now') 
                            WHERE [HotaUserId] = @id;",
                            new { id = hotaUser.HotaUserId, newName = hotaUser.DisplayName, elo = hotaUser.Elo, rep = hotaUser.Rep });
            }
            else
            {
                var model = new HotaUserData(hotaUser);
                HotaUsersById.Add(hotaUser.HotaUserId, model);
                HotaUsersByName.Add(hotaUser.DisplayName.ToLower(), model);

                EnqueueWorkItem(@$"INSERT INTO [{HotaUserDataTableName}] ([HotaUserId], [DisplayName], [Elo], [Rep], [LastUpdatedAtUtc]) 
                           VALUES (@id, @newName, @elo, @rep, datetime('now') );",
                           new { id = hotaUser.HotaUserId, newName = hotaUser.DisplayName, elo = hotaUser.Elo, rep = hotaUser.Rep });
            }
        }

        public void UpdateElo(int id, int newElo)
        {
            if (!HotaUsersById.TryGetValue(id, out var user))
            {
                return;
            }

            user.Elo = newElo;

            EnqueueWorkItem(@$"UPDATE [{HotaUserDataTableName}] 
                        SET [Elo] = @newElo, [LastUpdatedAtUtc] = datetime('now') 
                        WHERE [HotaUserId] = @id;", new { id, newElo });
        }

        public void UpdateRep(int id, int newRep)
        {
            if (!HotaUsersById.TryGetValue(id, out var user))
            {
                return;
            }

            user.Rep = newRep;

            EnqueueWorkItem(@$"UPDATE [{HotaUserDataTableName}] 
                        SET [Rep] = @newRep, [LastUpdatedAtUtc] = datetime('now') 
                        WHERE [HotaUserId] = @id;", new { id, newRep });
        }

        public HotaUser? GetUser(int userId)
        {
            if (!HotaUsersById.TryGetValue(userId, out var user))
            {
                return null;
            }
            return new HotaUser(user.HotaUserId, user.DisplayName, user.Elo, user.Rep);
        }
    }
}
