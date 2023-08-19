using System;
using System.Collections.Generic;
using zsemlebot.repository.Models;

namespace zsemlebot.repository
{
    public class TwitchRepository : RepositoryBase
    {
        private Dictionary<int, TwitchUserData> TwitchUsersById { get; set; }
        private Dictionary<string, TwitchUserData> TwitchUsersByName { get; set; }

        public TwitchRepository()
        {
            TwitchUsersById = new Dictionary<int, TwitchUserData>();
            TwitchUsersByName = new Dictionary<string, TwitchUserData>();

            LoadTwitchUsers();
        }

        private void LoadTwitchUsers()
        {
            var models = Query<TwitchUserData>($"SELECT [TwitchUserId], [DisplayName] FROM [{TwitchUserDataTableName}];");
            foreach (var model in models)
            {
                TwitchUsersById.Add(model.TwitchUserId, model);
                TwitchUsersByName.Add(model.DisplayName, model);
            }
        }

        public void UpdateTwitchUserName(int id, string newName)
        {
            if (TwitchUsersById.TryGetValue(id, out var oldUser))
            {
                if (oldUser.DisplayName == newName)
                {
                    return;
                }

                var newUser = new TwitchUserData { TwitchUserId = id, DisplayName = newName };
                TwitchUsersById[id] = newUser;
                TwitchUsersByName.Remove(oldUser.DisplayName);
                TwitchUsersByName.Add(newName, newUser);

                Execute(@$"UPDATE [{TwitchUserDataTableName}] 
                            SET [DisplayName] = @newName 
                            WHERE [TwitchUserId] = @id;", new {id, newName });
            }
            else
            {
                var newUser = new TwitchUserData { TwitchUserId = id, DisplayName = newName };
                TwitchUsersById.Add(id, newUser);
                TwitchUsersByName.Add(newName, newUser);

                Execute(@$"INSERT INTO [{TwitchUserDataTableName}] ([TwitchUserId], [DisplayName]) 
                            VALUES (@id, @newName);", new { id, newName });
            }
        }
    }
}
