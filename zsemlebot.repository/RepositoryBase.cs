using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using zsemlebot.core;

namespace zsemlebot.repository
{
    public abstract class RepositoryBase
    {
        protected const string ChannelSettingsTableName = "ChannelSettings";
        protected const string JoinedChannelsTableName = "JoinedChannels";
        protected const string TwitchUserDataTableName = "TwitchUserData";

        static RepositoryBase()
        {
            BackupDatabaseFile();

            CreateTables();
        }

        private static void CreateTables()
        {
            ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS [ChannelSettings]
				(
					[Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					[ChannelName] TEXT NOT NULL,
					[SettingName] TEXT NOT NULL,
					[SettingValue] TEXT NOT NULL
				)");

            ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS [JoinedChannels]
				(
					[TwitchUserId] INTEGER NOT NULL PRIMARY KEY
				)");

            ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS [TwitchUserData]
				(
					[TwitchUserId] INTEGER NOT NULL PRIMARY KEY,
					[DisplayName] TEXT NOT NULL
				)");

            ExecuteNonQuery(@"CREATE INDEX IF NOT EXISTS [IX_TwitchUserData_TwitchUserId] ON [TwitchUserData] 
                (
					[TwitchUserId]
				)");
        }

        protected static int GetLastRowId()
        {
            return QueryFirst<int>("SELECT last_insert_rowid()");
        }

        protected static void ExecuteNonQuery(string sql, object param = null)
        {
            using var connection = GetConnection();

            connection.Execute(sql, param);
        }

        protected static IReadOnlyList<T> Query<T>(string sql, object param = null)
        {
            using var connection = GetConnection();

            return connection.Query<T>(sql, param).ToList();
        }

        protected static IReadOnlyList<TReturn> Query<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, string splitOn, object param = null)
        {
            using var connection = GetConnection();
            return connection.Query(sql, map, splitOn: splitOn, param: param).ToList();
        }

        protected static T QueryFirst<T>(string sql, object param = null)
        {
            using var connection = GetConnection();

            return connection.QueryFirst<T>(sql, param);
        }

        protected static T QueryFirstOrDefault<T>(string sql, object param = null)
        {
            using var connection = GetConnection();

            return connection.QueryFirstOrDefault<T>(sql, param);
        }

        protected static TReturn QueryFirstOrDefault<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, string splitOn, object param = null)
        {
            using var connection = GetConnection();

            return connection.Query(sql, map, splitOn: splitOn, param: param).FirstOrDefault();
        }

        protected static bool QueryHasResult(string sql, object param = null)
        {
            using var connection = GetConnection();

            var result = connection.ExecuteScalar(sql, param);

            return result != null;
        }

        protected static int Execute(string sql, object param = null)
        {
            using var connection = GetConnection();
            var result = connection.Execute(sql, param);
            return result;
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
                var dbFilename = Configuration.Instance.Global.FullDatabaseFilePath;

                var dbBackupName = $"{Configuration.Instance.Global.DatabaseFileName}_{DateTime.Now:yyyyMMdd_HHmmss}";
                var dbBackupFilename = Path.Combine(Configuration.Instance.Global.FullDbBackupDirectory, dbBackupName);

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
            var dbDirectory = Configuration.Instance.Global.FullDbDirectory;
            var dbFilename = Configuration.Instance.Global.FullDatabaseFilePath;

            var builder = new SQLiteConnectionStringBuilder();
            builder.DataSource = dbFilename;
            builder.Version = 3;
            builder.Pooling = true;

            return builder.ToString();
        }
    }
}
