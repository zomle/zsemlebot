using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Threading;
using zsemlebot.core;

namespace zsemlebot.repository
{
	public abstract class ZsemlebotRepositoryBase : RepositoryBase, IDisposable
	{
		protected const string ChannelSettingsTableName = "ChannelSettings";
		protected const string JoinedChannelsTableName = "JoinedChannels";
		protected const string TwitchUserDataTableName = "TwitchUserData";
		protected const string HotaUserDataTableName = "HotaUserData";
		protected const string TwitchHotaUserLinkTableName = "TwitchHotaUserLink";
		protected const string TwitchHotaUserLinkRequestTableName = "TwitchHotaUserLinkRequest";

		private static readonly Queue<DatabaseWorkItem> WorkItemQueue;
		private static readonly object padlock;
		protected static bool DisposedValue { get; private set; }

		protected ZsemlebotRepositoryBase()
			: base()
		{
			BackupDatabaseFile();
			CreateTables();

			CleanUpOldData();
		}

		static ZsemlebotRepositoryBase()
		{
			padlock = new object();
			WorkItemQueue = new Queue<DatabaseWorkItem>();

			DisposedValue = false;

			StartUpdateThread();
		}

		public static void DisposeStatic()
		{
			DisposedValue = true;
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

		private static void StartUpdateThread()
		{
			new Thread(UpdateThreadWorker).Start();
		}

		private static void UpdateThreadWorker()
		{
			while (!DisposedValue)
			{
				var workItems = new List<DatabaseWorkItem>();
				lock (padlock)
				{
					while (WorkItemQueue.TryDequeue(out var tmp))
					{
						workItems.Add(tmp);
					}
				}

				if (workItems.Count > 0)
				{
					ExecuteInTransaction(workItems);
				}
				Thread.Sleep(500);
			}
		}

		protected void EnqueueWorkItem(string sql, object? param = null)
		{
			if (DisposedValue)
			{
				throw new InvalidOperationException("Repository is already disposed.");
			}

			lock (padlock)
			{
				WorkItemQueue.Enqueue(new DatabaseWorkItem(sql, param));
			}
		}

		protected static void ExecuteInTransaction(IEnumerable<DatabaseWorkItem> workItems)
		{
			using var connection = GetConnectionStatic();
			using var transaction = connection.BeginTransaction();

			foreach (var workItem in workItems)
			{
				connection.Execute(workItem.Query, workItem.Parameters, transaction);
			}

			transaction.Commit();
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

		protected override SQLiteConnection GetConnection()
		{
			return GetConnectionStatic();
		}

		private static SQLiteConnection GetConnectionStatic()
		{
			var connectionString = GetConnectionString(Config.Instance.Global.FullDatabaseFilePath);

			var connection = new SQLiteConnection(connectionString);
			connection.Open();

			return connection;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!DisposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				DisposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
