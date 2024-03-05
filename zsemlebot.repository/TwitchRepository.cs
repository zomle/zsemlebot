using System;
using System.Collections.Generic;
using zsemlebot.core;
using zsemlebot.core.Domain;
using zsemlebot.repository.Models;

namespace zsemlebot.repository
{
    public class TwitchRepository : ZsemlebotRepositoryBase
	{
        public static readonly TwitchRepository Instance;

        private Dictionary<int, TwitchUserData> TwitchUsersById { get; set; }
        private Dictionary<string, TwitchUserData> TwitchUsersByName { get; set; }

        static TwitchRepository()
        {
            Instance = new TwitchRepository();
        }

        private TwitchRepository() 
			: base(Config.Instance.Global.FullDatabaseFilePath)
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

				if (TwitchUsersByName.TryGetValue(model.DisplayName.ToLower(), out var existingUserData))
				{
					if (existingUserData.TwitchUserId > model.TwitchUserId)
					{
						continue;
					}
					else
					{
						TwitchUsersByName.Remove(model.DisplayName.ToLower());
					}
				}
				TwitchUsersByName.Add(model.DisplayName.ToLower(), model);
            }
        }

        public TwitchUser GetUser(int userId)
        {
            var userData = TwitchUsersById[userId];
            return new TwitchUser(userData.TwitchUserId, userData.DisplayName);
        }

        public TwitchUser? GetUser(string userName)
        {
            if (!TwitchUsersByName.TryGetValue(userName.ToLower(), out var userData))
            {
                return null;
            }

            return new TwitchUser(userData.TwitchUserId, userData.DisplayName);
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
                TwitchUsersByName.Remove(oldUser.DisplayName.ToLower());
                TwitchUsersByName.Add(newName.ToLower(), newUser);

                EnqueueWorkItem(@$"UPDATE [{TwitchUserDataTableName}] 
                            SET [DisplayName] = @newName 
                            WHERE [TwitchUserId] = @id;", new {id, newName });
            }
            else
            {
                var newUser = new TwitchUserData { TwitchUserId = id, DisplayName = newName };
                TwitchUsersById.Add(id, newUser);

				if (TwitchUsersByName.TryGetValue(newName.ToLower(), out var existingUserData)) 
				{
					if (existingUserData.TwitchUserId > newUser.TwitchUserId)
					{
						//if the existing user id is larger than the new one, then ignore the new one, stick with the existing user data.
						return;
					}
					else
					{
						TwitchUsersByName.Remove(newName.ToLower());
					}
				}

                TwitchUsersByName.Add(newName.ToLower(), newUser);

                EnqueueWorkItem(@$"INSERT INTO [{TwitchUserDataTableName}] ([TwitchUserId], [DisplayName]) 
                            VALUES (@id, @newName);", new { id, newName });
            }
        }
    }
}
