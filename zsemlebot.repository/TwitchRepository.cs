using System;
using System.Collections.Generic;
using zsemlebot.repository.Models;

namespace zsemlebot.repository
{
    public class TwitchRepository : RepositoryBase
    {
        private Dictionary<int, string> TwitchUsersById { get; set; }
        private Dictionary<string, int> TwitchUsersByName { get; set; }

        public TwitchRepository()
        {
            TwitchUsersById = new Dictionary<int, string>();
            TwitchUsersByName = new Dictionary<string, int>();

            LoadTwitchUsers();
        }

        private void LoadTwitchUsers()
        {
            var models = Query<TwitchUserData>("SELECT [TwitchUserId], [DisplayName] FROM [TwitchUserData];");
            foreach (var model in models)
            {
                TwitchUsersById.Add(model.TwitchUserId, model.DisplayName);
                TwitchUsersByName.Add(model.DisplayName, model.TwitchUserId);
            }
        }

        public void UpdateTwitchUserName(int id, string newName)
        {
            if (TwitchUsersById.TryGetValue(id, out var oldName))
            {
                if (oldName == newName)
                {
                    return;
                }

                TwitchUsersById[id] = newName;
                TwitchUsersByName.Remove(oldName);
                TwitchUsersByName.Add(newName, id);

                Execute("UPDATE [TwitchUserData] SET [DisplayName] = @newname WHERE [TwitchUserId] = @id;", new { id = id, newname = newName });
            }
            else
            {
                TwitchUsersById.Add(id, newName);
                TwitchUsersByName.Add(newName, id);

                Execute("INSERT INTO [TwitchUserData] ([DisplayName], [TwitchUserId]) VALUES (@newname, @id);", new { id = id, newname = newName });
            }
        }
    }
}
