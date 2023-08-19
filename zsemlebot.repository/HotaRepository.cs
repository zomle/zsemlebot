using System;
using System.Collections.Generic;
using zsemlebot.repository.Models;

namespace zsemlebot.repository
{
    public class HotaRepository : RepositoryBase
    {
        private Dictionary<int, HotaUserData> HotaUsersById { get; set; }
        private Dictionary<string, HotaUserData> HotaUsersByName { get; set; }

        public HotaRepository()
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
                HotaUsersByName.Add(model.DisplayName, model);
            }
        }

        public void UpdateHotaUser(int id, string newName, int elo, int rep)
        {
            if (HotaUsersById.TryGetValue(id, out var oldUser))
            {
                if (oldUser.DisplayName == newName && oldUser.Elo == elo && oldUser.Rep == rep)
                {
                    return;
                }


                if (oldUser.DisplayName != newName)
                {
                    var newUser = new HotaUserData { HotaUserId = id, DisplayName = newName, Elo = elo, Rep = rep };
                    HotaUsersById[id] = newUser;
                    HotaUsersByName.Remove(oldUser.DisplayName);
                    HotaUsersByName.Add(newName, newUser);
                }

                EnqueueWorkItem(@$"UPDATE [{HotaUserDataTableName}] 
                            SET [DisplayName] = @newName, [Elo] = @elo, [Rep] = @rep, [LastUpdatedAtUtc] = datetime('now') 
                            WHERE [HotaUserId] = @id;", new { id, newName, elo, rep });
            }
            else
            {
                var newUser = new HotaUserData { HotaUserId = id, DisplayName = newName, Elo = elo, Rep = rep };
                HotaUsersById.Add(id, newUser);
                HotaUsersByName.Add(newName, newUser);

                EnqueueWorkItem(@$"INSERT INTO [{HotaUserDataTableName}] ([HotaUserId], [DisplayName], [Elo], [Rep], [LastUpdatedAtUtc]) 
                           VALUES (@id, @newName, @elo, @rep, datetime('now') );", new { id, newName, elo, rep });
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
    }
}
