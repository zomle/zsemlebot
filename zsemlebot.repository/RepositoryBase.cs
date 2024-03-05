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
		protected RepositoryBase()
		{
		}

		protected abstract SQLiteConnection GetConnection();

		protected int GetLastRowId()
		{
			return QueryFirst<int>("SELECT last_insert_rowid()");
		}


		protected void ExecuteNonQuery(string sql, object? param = null)
		{
			using var connection = GetConnection();

			connection.Execute(sql, param);
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

		protected static string GetConnectionString(string databaseFilePath)
		{
			var builder = new SQLiteConnectionStringBuilder
			{
				DataSource = databaseFilePath,
				Version = 3,
				Pooling = true
			};

			return builder.ToString();
		}
	}
}
