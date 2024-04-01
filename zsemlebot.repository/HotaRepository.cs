using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using zsemlebot.core;
using zsemlebot.core.Domain;
using zsemlebot.core.Enums;
using zsemlebot.repository.Models;

namespace zsemlebot.repository
{
	public class HotaRepository : ZsemlebotRepositoryBase
	{
		public static readonly HotaRepository Instance;

		private Dictionary<uint, HotaUserData> HotaUsersById { get; set; }
		private Dictionary<string, HotaUserData> HotaUsersByName { get; set; }
		private Dictionary<uint, HotaMapData> HotaMapsById { get; set; }

		static HotaRepository()
		{
			Instance = new HotaRepository();
		}

		private HotaRepository()
		{
			HotaUsersById = new Dictionary<uint, HotaUserData>();
			HotaUsersByName = new Dictionary<string, HotaUserData>();
			HotaMapsById = new Dictionary<uint, HotaMapData>();

			LoadHotaUsers();
			LoadHotaMaps();
		}

		private void LoadHotaUsers()
		{
			var models = Query<HotaUserData>($"SELECT [HotaUserId], [DisplayName], [Elo], [Rep], [LastUpdatedAtUtc] FROM [{HotaUserDataTableName}];");
			foreach (var model in models)
			{
				HotaUsersById.Add(model.HotaUserId, model);

				if (HotaUsersByName.TryGetValue(model.DisplayName.ToLower(), out var existingUserData))
				{
					if (existingUserData.HotaUserId > model.HotaUserId)
					{
						continue;
					}
					else
					{
						HotaUsersByName.Remove(model.DisplayName.ToLower());
					}
				}
				HotaUsersByName.Add(model.DisplayName.ToLower(), model);
			}
		}

		private void LoadHotaMaps()
		{
			var models = Query<HotaMapData>($"SELECT [HotaMapId], [MapName] FROM [{HotaMapDataTableName}];");
			foreach (var model in models)
			{
				HotaMapsById.Add(model.HotaMapId, model);
			}
		}

		public HotaMap? GetMap(uint mapId)
		{
			if (HotaMapsById.TryGetValue(mapId, out var model))
			{
				return new HotaMap(model.HotaMapId, model.MapName);
			}

			return null;
		}

		public void UpdateMap(uint mapId, string mapName)
		{
			if (HotaMapsById.TryGetValue(mapId, out var oldMap))
			{
				if (oldMap.MapName != mapName)
				{
					oldMap.MapName = mapName;
				}

				EnqueueWorkItem(@$"UPDATE [{HotaMapDataTableName}] 
                            SET [MapName] = @newName
                            WHERE [HotaMapId] = @id;",
							new { id = mapId, newName = mapName });
			}
			else
			{
				var model = new HotaMapData { HotaMapId = mapId, MapName = mapName };
				HotaMapsById.Add(mapId, model);

				EnqueueWorkItem(@$"INSERT INTO [{HotaMapDataTableName}] ([HotaMapId], [MapName]) 
                           VALUES (@id, @newName);",
						   new { id = mapId, newName = mapName });
			}
		}

		public void UpdateGameHistory(uint mainHotaUserId, IReadOnlyList<HotaUserGameHistoryEntry> entries)
		{
			if (!HotaUsersById.TryGetValue(mainHotaUserId, out var mainUser))
			{
				return;
			}

			foreach (var entry in entries)
			{
				mainUser.GameHistory[entry.GameId] = entry;
			}

			mainUser.LastUpdatedAtUtc = DateTime.UtcNow;
			mainUser.GameHistoryUpToDate = true;
		}

		public void InvalidateGameHistory(HotaGamePlayer player)
		{
			InvalidateGameHistory(player.HotaUser);
		}

		public void InvalidateGameHistory(HotaUser hotaUser)
		{
			hotaUser.GameHistoryUpToDate = false;

			if (!HotaUsersById.TryGetValue(hotaUser.HotaUserId, out var mainUser))
			{
				return;
			}

			mainUser.GameHistoryUpToDate = false;
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
					model.LastUpdatedAtUtc = DateTime.UtcNow;

					HotaUsersById[hotaUser.HotaUserId] = model;
					HotaUsersByName.Remove(oldUser.DisplayName.ToLower());
					HotaUsersByName.Add(hotaUser.DisplayName.ToLower(), model);
				}

				if (oldUser.Elo != hotaUser.Elo)
				{
					oldUser.Elo = hotaUser.Elo;
				}

				oldUser.LastUpdatedAtUtc = DateTime.UtcNow;

				EnqueueWorkItem(@$"UPDATE [{HotaUserDataTableName}] 
                            SET [DisplayName] = @newName, [Elo] = @elo, [Rep] = @rep, [LastUpdatedAtUtc] = datetime('now') 
                            WHERE [HotaUserId] = @id;",
							new { id = hotaUser.HotaUserId, newName = hotaUser.DisplayName, elo = hotaUser.Elo, rep = hotaUser.Rep });
			}
			else
			{
				var model = new HotaUserData(hotaUser);
				model.LastUpdatedAtUtc = DateTime.UtcNow;

				HotaUsersById.Add(hotaUser.HotaUserId, model);

				if (HotaUsersByName.TryGetValue(hotaUser.DisplayName.ToLower(), out var existingUserData))
				{
					if (existingUserData.HotaUserId > model.HotaUserId)
					{
						//if the existing user id is larger than the new one, then ignore the new one, stick with the existing user data.
						return;
					}
					else
					{
						HotaUsersByName.Remove(hotaUser.DisplayName.ToLower());
					}
				}

				HotaUsersByName.Add(hotaUser.DisplayName.ToLower(), model);

				EnqueueWorkItem(@$"INSERT INTO [{HotaUserDataTableName}] ([HotaUserId], [DisplayName], [Elo], [Rep], [LastUpdatedAtUtc]) 
                           VALUES (@id, @newName, @elo, @rep, datetime('now') );",
						   new { id = hotaUser.HotaUserId, newName = hotaUser.DisplayName, elo = hotaUser.Elo, rep = hotaUser.Rep });
			}
		}

		public void UpdateElo(uint id, int newElo)
		{
			if (!HotaUsersById.TryGetValue(id, out var user))
			{
				return;
			}

			user.Elo = newElo;
			user.LastUpdatedAtUtc = DateTime.UtcNow;
			EnqueueWorkItem(@$"UPDATE [{HotaUserDataTableName}] 
                        SET [Elo] = @newElo, [LastUpdatedAtUtc] = datetime('now') 
                        WHERE [HotaUserId] = @id;", new { id, newElo });
		}

		public void UpdateRep(uint id, int newRep)
		{
			if (!HotaUsersById.TryGetValue(id, out var user))
			{
				return;
			}

			user.Rep = newRep;
			user.LastUpdatedAtUtc = DateTime.UtcNow;

			EnqueueWorkItem(@$"UPDATE [{HotaUserDataTableName}] 
                        SET [Rep] = @newRep, [LastUpdatedAtUtc] = datetime('now') 
                        WHERE [HotaUserId] = @id;", new { id, newRep });
		}

		public HotaUser? GetUser(uint userId)
		{
			if (!HotaUsersById.TryGetValue(userId, out var userData))
			{
				return null;
			}

			return new HotaUser(userData.HotaUserId, userData.DisplayName, userData.Elo, userData.Rep, HotaUserStatus.Offline, userData.LastUpdatedAtUtc, userData.GameHistory, userData.GameHistoryUpToDate);
		}

		public HotaUser? GetUser(string userName)
		{
			if (!HotaUsersByName.TryGetValue(userName.ToLower(), out var userData))
			{
				return null;
			}

			return new HotaUser(userData.HotaUserId, userData.DisplayName, userData.Elo, userData.Rep, HotaUserStatus.Offline, userData.LastUpdatedAtUtc, userData.GameHistory, userData.GameHistoryUpToDate);
		}
	}
}
