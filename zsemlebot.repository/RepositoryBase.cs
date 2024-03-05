using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading;

namespace zsemlebot.repository
{
	public abstract class RepositoryBase
	{
		private readonly Queue<DatabaseWorkItem> WorkItemQueue;
		private bool Disposing;

		private static readonly object padlock;

		private string DatabaseFilePath { get; }

		protected RepositoryBase(string databaseFilePath)
		{
			WorkItemQueue = new Queue<DatabaseWorkItem>();

			DatabaseFilePath = databaseFilePath;
			Disposing = false;
			StartUpdateThread();
		}

		static RepositoryBase()
		{
			padlock = new object();
		}

		public void Dispose()
		{
			Disposing = true;
		}

		protected int GetLastRowId()
		{
			return QueryFirst<int>("SELECT last_insert_rowid()");
		}

		private void StartUpdateThread()
		{
			new Thread(UpdateThreadWorker).Start();
		}

		private void UpdateThreadWorker()
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

		protected void ExecuteNonQuery(string sql, object? param = null)
		{
			using var connection = GetConnection();

			connection.Execute(sql, param);
		}
		protected void EnqueueWorkItem(string sql, object? param = null)
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

		protected IReadOnlyList<T> Query<T>(string sql, object? param = null)
		{
			using var connection = GetConnection();

			return connection.Query<T>(sql, param).ToList();
		}

		protected IReadOnlyList<TReturn> Query<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, string splitOn, object? param = null)
		{
			using var connection = GetConnection();
			return connection.Query(sql, map, splitOn: splitOn, param: param).ToList();
		}

		protected T QueryFirst<T>(string sql, object? param = null)
		{
			using var connection = GetConnection();

			return connection.QueryFirst<T>(sql, param);
		}

		protected T QueryFirstOrDefault<T>(string sql, object? param = null)
		{
			using var connection = GetConnection();

			return connection.QueryFirstOrDefault<T>(sql, param);
		}

		protected TReturn? QueryFirstOrDefault<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, string splitOn, object? param = null)
		{
			using var connection = GetConnection();

			return connection.Query(sql, map, splitOn: splitOn, param: param).FirstOrDefault();
		}

		protected bool QueryHasResult(string sql, object? param = null)
		{
			using var connection = GetConnection();

			var result = connection.ExecuteScalar(sql, param);

			return result != null;
		}

		protected int Execute(string sql, object? param = null)
		{
			using var connection = GetConnection();
			var result = connection.Execute(sql, param);
			return result;
		}

		private void ExecuteInTransaction(IEnumerable<DatabaseWorkItem> workItems)
		{
			using var connection = GetConnection();
			using var transaction = connection.BeginTransaction();

			foreach (var workItem in workItems)
			{
				connection.Execute(workItem.Query, workItem.Parameters, transaction);
			}

			transaction.Commit();
		}

		private SQLiteConnection GetConnection()
		{
			var connectionString = GetConnectionString();

			var connection = new SQLiteConnection(connectionString);
			connection.Open();

			return connection;
		}

		private string GetConnectionString()
		{
			var builder = new SQLiteConnectionStringBuilder
			{
				DataSource = DatabaseFilePath,
				Version = 3,
				Pooling = true
			};

			return builder.ToString();
		}
	}
}
