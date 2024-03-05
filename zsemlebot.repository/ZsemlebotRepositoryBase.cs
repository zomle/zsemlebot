using System;
using System.Diagnostics;
using System.IO;
using zsemlebot.core;

namespace zsemlebot.repository
{
	public abstract class ZsemlebotRepositoryBase : RepositoryBase
	{
		protected const string ChannelSettingsTableName = "ChannelSettings";
		protected const string JoinedChannelsTableName = "JoinedChannels";
		protected const string TwitchUserDataTableName = "TwitchUserData";
		protected const string HotaUserDataTableName = "HotaUserData";
		protected const string TwitchHotaUserLinkTableName = "TwitchHotaUserLink";
		protected const string TwitchHotaUserLinkRequestTableName = "TwitchHotaUserLinkRequest";

		protected ZsemlebotRepositoryBase(string databaseFilePath)
			: base(databaseFilePath)
		{
			BackupDatabaseFile();
			CreateTables();

			CleanUpOldData();
		}

		private void CreateTables()
		{
			ExecuteNonQuery(@$"CREATE TABLE IF NOT EXISTS [{ChannelSettingsTableName}]
				(
					[Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					[ChannelName] TEXT NOT NULL,
					[SettingName] TEXT NOT NULL,
					[SettingValue] TEXT NOT NULL
				)");

			ExecuteNonQuery(@$"CREATE TABLE IF NOT EXISTS [{JoinedChannelsTableName}]
				(
					[TwitchUserId] INTEGER NOT NULL PRIMARY KEY
				)");

			ExecuteNonQuery(@$"CREATE TABLE IF NOT EXISTS [{TwitchUserDataTableName}]
				(
					[TwitchUserId] INTEGER NOT NULL PRIMARY KEY,
					[DisplayName] TEXT NOT NULL COLLATE NOCASE
				)");

			ExecuteNonQuery(@$"CREATE INDEX IF NOT EXISTS [IX_TwitchUserData_TwitchUserId] ON [{TwitchUserDataTableName}] 
                (
					[TwitchUserId]
				)");

			ExecuteNonQuery(@$"CREATE TABLE IF NOT EXISTS [{HotaUserDataTableName}]
				(
					[HotaUserId] INTEGER NOT NULL PRIMARY KEY,
					[DisplayName] TEXT NOT NULL COLLATE NOCASE,
                    [Elo] INTEGER,
                    [Rep] INTEGER,
                    [LastUpdatedAtUtc] TEXT NOT NULL
				)");

			ExecuteNonQuery(@$"CREATE INDEX IF NOT EXISTS [IX_HotaUserData_HotaUserId] ON [{HotaUserDataTableName}] 
                (
					[HotaUserId]
				)");

			ExecuteNonQuery(@$"CREATE TABLE IF NOT EXISTS [{TwitchHotaUserLinkTableName}]
				(
                    [TwitchUserId] INTEGER NOT NULL,
					[HotaUserId] INTEGER NOT NULL,
                    [CreatedAtUtc] TEXT NOT NULL
				)");

			ExecuteNonQuery(@$"CREATE TABLE IF NOT EXISTS [{TwitchHotaUserLinkRequestTableName}]
				(
                    [TwitchUserName] TEXT NOT NULL COLLATE NOCASE,
					[HotaUserId] INTEGER NOT NULL,
                    [AuthCode] TEXT NOT NULL,
                    [ValidUntilUtc] TEXT NOT NULL
				)");
		}

		private void CleanUpOldData()
		{
			ExecuteNonQuery(@$"DELETE FROM [{TwitchHotaUserLinkRequestTableName}] WHERE [ValidUntilUtc] < datetime('now');");
		}

		private static void BackupDatabaseFile()
		{
			try
			{
				var dbFilename = Config.Instance.Global.FullDatabaseFilePath;

				var dbBackupName = $"{Config.Instance.Global.DatabaseFileName}_{DateTime.Now:yyyyMMdd_HHmmss}";
				var dbBackupFilename = Path.Combine(Config.Instance.Global.FullDbBackupDirectory, dbBackupName);

				if (File.Exists(dbFilename))
				{
					File.Copy(dbFilename, dbBackupFilename);
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine("Failed to create backup: " + e.Message);
			}
		}
	}
}
