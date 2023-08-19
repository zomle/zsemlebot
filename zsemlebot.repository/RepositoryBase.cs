using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using zsemlebot.core;

namespace zsemlebot.repository
{
    public abstract class RepositoryBase
    {
        protected const string ChannelSettingsTableName = "ChannelSettings";
        protected const string JoinedChannelsTableName = "JoinedChannels";
        protected const string TwitchUserDataTableName = "TwitchUserData";
        protected const string HotaUserDataTableName = "HotaUserData";

        private static readonly Queue<DatabaseWorkItem> WorkItemQueue;
        private static readonly object padlock;
        private static bool Disposing;

        static RepositoryBase()
        {
            WorkItemQueue = new Queue<DatabaseWorkItem>();
            padlock = new object();
            Disposing = false;

            BackupDatabaseFile();
            CreateTables();

            StartUpdateThread();
        }

        public static void Dispose()
        {
            Disposing = true;
        }

        private static void CreateTables()
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
					[DisplayName] TEXT NOT NULL
				)");

            ExecuteNonQuery(@$"CREATE INDEX IF NOT EXISTS [IX_TwitchUserData_TwitchUserId] ON [{TwitchUserDataTableName}] 
                (
					[TwitchUserId]
				)");

            ExecuteNonQuery(@$"CREATE TABLE IF NOT EXISTS [{HotaUserDataTableName}]
				(
					[HotaUserId] INTEGER NOT NULL PRIMARY KEY,
					[DisplayName] TEXT NOT NULL,
                    [Elo] INTEGER,
                    [Rep] INTEGER,
                    [LastUpdatedAtUtc] TEXT NOT NULL
				)");

            ExecuteNonQuery(@$"CREATE INDEX IF NOT EXISTS [IX_HotaUserData_HotaUserId] ON [{HotaUserDataTableName}] 
                (
					[HotaUserId]
				)");
        }

        protected static int GetLastRowId()
        {
            return QueryFirst<int>("SELECT last_insert_rowid()");
        }

        private static void StartUpdateThread()
        {
            new Thread(UpdateThreadWorker).Start();
        }

        private static void UpdateThreadWorker()
        {
            while (!Disposing)
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

        protected static void ExecuteNonQuery(string sql, object? param = null)
        {
            using var connection = GetConnection();

            connection.Execute(sql, param);
        }
        protected static void EnqueueWorkItem(string sql, object? param = null)
        {
            if (Disposing)
            {
                throw new InvalidOperationException("Repository is already disposed.");
            }

            lock (padlock)
            {
                WorkItemQueue.Enqueue(new DatabaseWorkItem(sql, param));
            }
        }

        protected static IReadOnlyList<T> Query<T>(string sql, object? param = null)
        {
            using var connection = GetConnection();

            return connection.Query<T>(sql, param).ToList();
        }

        protected static IReadOnlyList<TReturn> Query<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, string splitOn, object? param = null)
        {
            using var connection = GetConnection();
            return connection.Query(sql, map, splitOn: splitOn, param: param).ToList();
        }

        protected static T QueryFirst<T>(string sql, object? param = null)
        {
            using var connection = GetConnection();

            return connection.QueryFirst<T>(sql, param);
        }

        protected static T QueryFirstOrDefault<T>(string sql, object? param = null)
        {
            using var connection = GetConnection();

            return connection.QueryFirstOrDefault<T>(sql, param);
        }

        protected static TReturn? QueryFirstOrDefault<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, string splitOn, object? param = null)
        {
            using var connection = GetConnection();

            return connection.Query(sql, map, splitOn: splitOn, param: param).FirstOrDefault();
        }

        protected static bool QueryHasResult(string sql, object? param = null)
        {
            using var connection = GetConnection();

            var result = connection.ExecuteScalar(sql, param);

            return result != null;
        }

        protected static int Execute(string sql, object? param = null)
        {
            using var connection = GetConnection();
            var result = connection.Execute(sql, param);
            return result;
        }

        private static void ExecuteInTransaction(IEnumerable<DatabaseWorkItem> workItems)
        {
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            foreach (var workItem in workItems)
            {
                connection.Execute(workItem.Query, workItem.Parameters, transaction);
            }

            transaction.Commit();
        }

        private static SQLiteConnection GetConnection()
        {
            var connectionString = GetConnectionString();

            var connection = new SQLiteConnection(connectionString);
            connection.Open();

            return connection;
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

        private static string GetConnectionString()
        {
            var dbFilename = Config.Instance.Global.FullDatabaseFilePath;

            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = dbFilename,
                Version = 3,
                Pooling = true
            };

            return builder.ToString();
        }
    }
}
