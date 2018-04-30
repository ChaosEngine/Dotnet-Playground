﻿using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Repositories
{
	public interface IHashesRepositoryPure
	{
		Task<HashesInfo> CurrentHashesInfo { get; }

		void SetReadOnly(bool value);

		Task<IEnumerable<ThinHashes>> AutoComplete(string text);

		Task<(IEnumerable<ThinHashes> Itemz, int Count)> SearchAsync(string sortColumn, string sortOrderDirection, string searchText,
			int offset, int limit, CancellationToken token);

		Task<HashesInfo> CalculateHashesInfo(ILoggerFactory _loggerFactory, ILogger _logger, IConfiguration conf,
			DbContextOptions<BloggingContext> dbContextOptions);

		Task<ThinHashes> SearchAsync(HashInput input);
	}

	public interface IHashesRepository : IGenericRepository<BloggingContext, ThinHashes>, IHashesRepositoryPure
	{
	}

	public class HashesRepository : GenericRepository<BloggingContext, ThinHashes>, IHashesRepository
	{
		private const string _NOTHING_FOUND_TEXT = "nothing found";
		/// <summary>
		/// Used value or this specific worker node/process or load balancing server
		/// </summary>
		private static HashesInfo _hashesInfoStatic;
		/// <summary>
		/// locally cached value for request, refreshed upon every request.
		/// </summary>
		private HashesInfo _hi;
		private static IEnumerable<string> _postgresAllColumnNames;
		private static readonly object _locker = new object();
		private readonly IConfiguration _configuration;

		public Task<HashesInfo> CurrentHashesInfo
		{
			get { return GetHashesInfoFromDB(_entities); }
		}

		private static IEnumerable<String> PostgresAllColumnNames
		{
			get
			{
				if (_postgresAllColumnNames == null)
					_postgresAllColumnNames = AllColumnNames.Select(x => x.Replace("Hash", "hash"));
				return _postgresAllColumnNames;
			}
		}

		private async Task<HashesInfo> GetHashesInfoFromDB(BloggingContext db)
		{
			if (_hashesInfoStatic == null)
			{
				if (_hi == null)            //local value is empty, fill it from DB once
					_hi = await db.HashesInfo.FirstOrDefaultAsync(x => x.ID == 0);

				if (_hi == null || _hi.IsCalculating)
					return _hi;             //still calculating, return just this local value
				else
					_hashesInfoStatic = _hi;//calculation ended, save to global static value
			}
			return _hashesInfoStatic;
		}

		public HashesRepository(BloggingContext context, IConfiguration configuration) : base(context)
		{
			_configuration = configuration;
		}

		public void SetReadOnly(bool value)
		{
			_entities.ChangeTracker.QueryTrackingBehavior = value ? QueryTrackingBehavior.NoTracking : QueryTrackingBehavior.TrackAll;
		}

		public async Task<IEnumerable<ThinHashes>> AutoComplete(string text)
		{
			text = $"{text.Trim().ToLower()}%";
			Task<List<ThinHashes>> found = null;

			switch (_entities.ConnectionTypeName)
			{
				case "sqliteconnection":
				case "mysqlconnection":
				case "sqlconnection":
					found = (from x in _entities.ThinHashes
							 where EF.Functions.Like(x.HashMD5, text) || EF.Functions.Like(x.HashSHA256, text)
							 select x)
						.Take(20)
						//.DefaultIfEmpty(new ThinHashes { Key = _NOTHING_FOUND_TEXT })
						.ToListAsync();
					break;

				case "npsqlconnection":
					found = (from x in _entities.ThinHashes
							 where EF.Functions.Like(x.HashMD5, text) || EF.Functions.Like(x.HashSHA256, text)
							 select x)
						.Take(20)
						//.DefaultIfEmpty(new ThinHashes { Key = _NOTHING_FOUND_TEXT })
						.ToListAsync();
					break;

				/*case "sqlconnection":
					found = _entities.ThinHashes.FromSql(
					$@"SELECT TOP 20 * FROM (
					SELECT x.[{nameof(Hashes.Key)}], x.{nameof(Hashes.HashMD5)}, x.{nameof(Hashes.HashSHA256)}
					FROM {nameof(Hashes)} AS x
					WHERE x.{nameof(Hashes.HashMD5)} like cast(@text as varchar)
					UNION ALL
					SELECT y.[{nameof(Hashes.Key)}], y.{nameof(Hashes.HashMD5)}, y.{nameof(Hashes.HashSHA256)}
					FROM {nameof(Hashes)} AS y
					WHERE y.{nameof(Hashes.HashSHA256)} like cast(@text as varchar)
				) a", new SqlParameter("@text", text + '%'))
						//.DefaultIfEmpty(new ThinHashes { Key = _NOTHING_FOUND_TEXT })
						.ToListAsync();
					break;*/

				default:
					throw new NotSupportedException($"Bad {nameof(BloggingContext.ConnectionTypeName)} name");
			}

			return (await found).DefaultIfEmpty(new ThinHashes { Key = _NOTHING_FOUND_TEXT });
		}

		private string WhereColumnCondition(char colNamePrefix, char colNameSuffix, IEnumerable<string> columnNames = null, string searchTextParamName = "searchText")
		{
			var sb = new StringBuilder(
@"
(
	");
			string comma = string.Empty;
			columnNames = columnNames ?? AllColumnNames;
			foreach (var col in columnNames)
			{
				//([Key] LIKE @searchText) OR
				sb.AppendFormat("{0}({3}{1}{4} LIKE @{2})", comma, col, searchTextParamName, colNamePrefix, colNameSuffix);
				comma = " OR" + Environment.NewLine + '\t';
			}
			sb.Append(@"
)
");
			return sb.ToString();
		}

		private async Task<(IEnumerable<ThinHashes> Itemz, int Count)> SearchSqlServerAsync(string sortColumn, string sortOrderDirection,
			string searchText, int offset, int limit, CancellationToken token)
		{
			string col_names = string.Join("],[", AllColumnNames);
			string sql =
(string.IsNullOrEmpty(searchText) ?
"SELECT rows FROM sysindexes WHERE id = OBJECT_ID('Hashes') AND indid < 2"
:
$@"
SELECT [{col_names}]
INTO #tempo
FROM Hashes
WHERE {WhereColumnCondition('[', ']')};
SELECT count(*) cnt FROM #tempo
"
) +
$@";
SELECT [{col_names}]
FROM {(string.IsNullOrEmpty(searchText) ? "Hashes" : "#tempo")}

{(string.IsNullOrEmpty(sortColumn) ? "ORDER BY 1" : $"ORDER BY [{sortColumn}] {sortOrderDirection}")}
OFFSET @offset ROWS
FETCH NEXT @limit ROWS ONLY
";

			var conn = _entities.Database.GetDbConnection();
			try
			{
				var found = new List<ThinHashes>(limit);
				int count = -1;

				await conn.OpenAsync(token);
				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = sql;
					cmd.CommandTimeout = 240;
					DbParameter parameter;

					parameter = cmd.CreateParameter();
					parameter.ParameterName = "@offset";
					parameter.DbType = DbType.Int32;
					parameter.Value = offset;
					cmd.Parameters.Add(parameter);

					parameter = cmd.CreateParameter();
					parameter.ParameterName = "@limit";
					parameter.DbType = DbType.Int32;
					parameter.Value = limit;
					cmd.Parameters.Add(parameter);

					if (!string.IsNullOrEmpty(searchText))
					{
						parameter = cmd.CreateParameter();
						parameter.ParameterName = "@searchText";
						parameter.DbType = DbType.String;
						parameter.Size = 100;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);
					}

					using (var rdr = await cmd.ExecuteReaderAsync(token))
					{
						if (await rdr.ReadAsync(token))
						{
							count = rdr.GetInt32(0);
						}

						if (count > 0 && await rdr.NextResultAsync(token) && rdr.HasRows)
						{
							string[] strings = new string[3];
							while (await rdr.ReadAsync(token))
							{
								rdr.GetValues(strings);
								found.Add(new ThinHashes
								{
									Key = strings[0],
									HashMD5 = strings[1],
									HashSHA256 = strings[2]
								});
							}
						}
						else
						{
							found.Add(new ThinHashes { Key = _NOTHING_FOUND_TEXT });
						}
					}
				}

				return (found, count);
			}
			finally
			{
				conn.Close();
			}
		}

		private async Task<(List<ThinHashes> Itemz, int Count)> SearchMySqlAsync(string sortColumn, string sortOrderDirection, string searchText,
			int offset, int limit, CancellationToken token)
		{
			string col_names = string.Join("`,`", AllColumnNames);
			string sql =// "SET SESSION SQL_BIG_SELECTS=1;" +
(string.IsNullOrEmpty(searchText) ?
@"
SELECT count(*) cnt FROM Hashes"
:
$@"
CREATE TEMPORARY TABLE tempo AS
SELECT `{col_names}`
FROM Hashes
WHERE {WhereColumnCondition('`', '`')}
;
SELECT count(*) cnt FROM tempo"
) +
$@";
SELECT `{col_names}`
FROM {(string.IsNullOrEmpty(searchText) ? "Hashes" : "tempo")}

{(string.IsNullOrEmpty(sortColumn) ? "" : $"ORDER BY `{sortColumn}` {sortOrderDirection}")}
LIMIT @limit OFFSET @offset
";

			using (var conn = new MySqlConnection(_configuration.GetConnectionString("MySQL")))
			{
				var found = new List<ThinHashes>(limit);
				int count = -1;

				await conn.OpenAsync(token);
				using (var cmd = new MySqlCommand(sql, conn))
				{
					cmd.CommandText = sql;
					cmd.CommandTimeout = 240;
					DbParameter parameter;

					parameter = cmd.CreateParameter();
					parameter.ParameterName = "@offset";
					parameter.DbType = DbType.Int32;
					parameter.Value = offset;
					cmd.Parameters.Add(parameter);

					parameter = cmd.CreateParameter();
					parameter.ParameterName = "@limit";
					parameter.DbType = DbType.Int32;
					parameter.Value = limit;
					cmd.Parameters.Add(parameter);

					if (!string.IsNullOrEmpty(searchText))
					{
						parameter = cmd.CreateParameter();
						parameter.ParameterName = "@searchText";
						parameter.DbType = DbType.String;
						parameter.Size = 100;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);
					}

					using (var rdr = await cmd.ExecuteReaderAsync(token))
					{
						if (await rdr.ReadAsync(token))
						{
							count = rdr.GetInt32(0);
						}

						if (count > 0 && await rdr.NextResultAsync(token) && rdr.HasRows)
						{
							string[] strings = new string[3];
							while (await rdr.ReadAsync(token))
							{
								rdr.GetValues(strings);
								found.Add(new ThinHashes
								{
									Key = strings[0],
									HashMD5 = strings[1],
									HashSHA256 = strings[2]
								});
							}
						}
						else
						{
							found.Add(new ThinHashes { Key = _NOTHING_FOUND_TEXT });
						}
					}
				}

				return (found, count);
			}//end using
		}

		private async Task<(IEnumerable<ThinHashes> Itemz, int Count)> SearchSqliteAsync(string sortColumn, string sortOrderDirection,
			string searchText, int offset, int limit, CancellationToken token)
		{
			string col_names = string.Join("],[", AllColumnNames);
			string sql =
(string.IsNullOrEmpty(searchText) ?
"SELECT count(*) cnt FROM Hashes"
:
$@"
CREATE TEMPORARY TABLE tempo AS
SELECT [{col_names}]
FROM Hashes
WHERE {WhereColumnCondition('[', ']')}
;
SELECT count(*) cnt FROM tempo"
) +
$@";
SELECT [{col_names}]
FROM {(string.IsNullOrEmpty(searchText) ? "Hashes" : "tempo")}

{(string.IsNullOrEmpty(sortColumn) ? "" : $"ORDER BY [{sortColumn}] {sortOrderDirection}")}
LIMIT @limit OFFSET @offset
";

			var conn = _entities.Database.GetDbConnection();
			try
			{
				var found = new List<ThinHashes>(limit);
				int count = -1;

				await conn.OpenAsync(token);
				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = sql;
					cmd.CommandTimeout = 240;
					DbParameter parameter;

					parameter = cmd.CreateParameter();
					parameter.ParameterName = "@offset";
					parameter.DbType = DbType.Int32;
					parameter.Value = offset;
					cmd.Parameters.Add(parameter);

					parameter = cmd.CreateParameter();
					parameter.ParameterName = "@limit";
					parameter.DbType = DbType.Int32;
					parameter.Value = limit;
					cmd.Parameters.Add(parameter);

					if (!string.IsNullOrEmpty(searchText))
					{
						parameter = cmd.CreateParameter();
						parameter.ParameterName = "@searchText";
						parameter.DbType = DbType.String;
						parameter.Size = 100;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);
					}

					using (var rdr = await cmd.ExecuteReaderAsync(token))
					{
						if (await rdr.ReadAsync(token))
						{
							count = rdr.GetInt32(0);
						}

						if (count > 0 && await rdr.NextResultAsync(token) && rdr.HasRows)
						{
							string[] strings = new string[3];
							while (await rdr.ReadAsync(token))
							{
								rdr.GetValues(strings);
								found.Add(new ThinHashes
								{
									Key = strings[0],
									HashMD5 = strings[1],
									HashSHA256 = strings[2]
								});
							}
						}
						else
						{
							found.Add(new ThinHashes { Key = _NOTHING_FOUND_TEXT });
						}
					}
				}

				return (found, count);
			}
			finally
			{
				conn.Close();
			}
		}

		private async Task<(IEnumerable<ThinHashes> Itemz, int Count)> SearchPostgresAsync(string sortColumn, string sortOrderDirection, string searchText, int offset, int limit, CancellationToken token)
		{
			string col_names = string.Join("\",\"", PostgresAllColumnNames);
			string sql =// "SET SESSION SQL_BIG_SELECTS=1;" +
(string.IsNullOrEmpty(searchText) ?
@"
SELECT count(*) cnt FROM ""Hashes"""
:
$@"
CREATE TEMPORARY TABLE tempo AS
SELECT ""{col_names}""
FROM ""Hashes""
WHERE {WhereColumnCondition('"', '"', PostgresAllColumnNames)}
;
SELECT count(*) cnt FROM tempo"
) +
$@";
SELECT ""{col_names}""
FROM {(string.IsNullOrEmpty(searchText) ? "\"Hashes\"" : "tempo")}

{(string.IsNullOrEmpty(sortColumn) ? "" : $"ORDER BY \"{PostgresAllColumnNames.FirstOrDefault(x => string.Compare(x, sortColumn, StringComparison.CurrentCultureIgnoreCase) == 0)}\" {sortOrderDirection}")}
LIMIT @limit OFFSET @offset
";

			using (var conn = new NpgsqlConnection(_configuration.GetConnectionString("PostgreSql")))
			{
				conn.ProvideClientCertificatesCallback = BloggingContextFactory.MyProvideClientCertificatesCallback;

				var found = new List<ThinHashes>(limit);
				int count = -1;

				await conn.OpenAsync(token);
				using (var cmd = new NpgsqlCommand(sql, conn))
				{
					cmd.CommandText = sql;
					cmd.CommandTimeout = 240;
					DbParameter parameter;

					parameter = cmd.CreateParameter();
					parameter.ParameterName = "@offset";
					parameter.DbType = DbType.Int32;
					parameter.Value = offset;
					cmd.Parameters.Add(parameter);

					parameter = cmd.CreateParameter();
					parameter.ParameterName = "@limit";
					parameter.DbType = DbType.Int32;
					parameter.Value = limit;
					cmd.Parameters.Add(parameter);

					if (!string.IsNullOrEmpty(searchText))
					{
						parameter = cmd.CreateParameter();
						parameter.ParameterName = "@searchText";
						parameter.DbType = DbType.String;
						parameter.Size = 100;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);
					}

					using (var rdr = await cmd.ExecuteReaderAsync(token))
					{
						if (await rdr.ReadAsync(token))
						{
							count = rdr.GetInt32(0);
						}

						if (count > 0 && await rdr.NextResultAsync(token) && rdr.HasRows)
						{
							string[] strings = new string[3];
							while (await rdr.ReadAsync(token))
							{
								rdr.GetValues(strings);
								found.Add(new ThinHashes
								{
									Key = strings[0],
									HashMD5 = strings[1],
									HashSHA256 = strings[2]
								});
							}
						}
						else
						{
							found.Add(new ThinHashes { Key = _NOTHING_FOUND_TEXT });
						}
					}
				}

				return (found, count);
			}//end using
		}

		public async Task<(IEnumerable<ThinHashes> Itemz, int Count)> SearchAsync(string sortColumn, string sortOrderDirection, string searchText,
			int offset, int limit, CancellationToken token)
		{
			if (!string.IsNullOrEmpty(sortColumn) && !AllColumnNames.Contains(sortColumn))
			{
				throw new ArgumentException("bad sort column");
			}
			else if (!string.IsNullOrEmpty(sortOrderDirection) &&
				   sortOrderDirection != "asc" && sortOrderDirection != "desc")
			{
				throw new ArgumentException("bad sort direction");
			}

			switch (_entities.ConnectionTypeName)
			{
				case "mysqlconnection":
					return await SearchMySqlAsync(sortColumn, sortOrderDirection, searchText, offset, limit, token);

				case "sqlconnection":
					return await SearchSqlServerAsync(sortColumn, sortOrderDirection, searchText, offset, limit, token);

				case "sqliteconnection":
					return await SearchSqliteAsync(sortColumn, sortOrderDirection, searchText, offset, limit, token);

				case "npsqlconnection":
					return await SearchPostgresAsync(sortColumn, sortOrderDirection, searchText, offset, limit, token);

				default:
					throw new NotSupportedException($"Bad {nameof(BloggingContext.ConnectionTypeName)} name");
			}
		}

		/// <summary>
		/// Sync running method
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="loggerFactory"></param>
		/// <param name="logger"></param>
		/// <param name="configuration"></param>
		/// <returns></returns>
		public async Task<HashesInfo> CalculateHashesInfo(ILoggerFactory loggerFactory, ILogger logger, IConfiguration configuration,
			DbContextOptions<BloggingContext> dbContextOptions)
		{
			HashesInfo hi = null;

			//var bc = new DbContextOptionsBuilder<BloggingContext>();
			//bc.UseLoggerFactory(loggerFactory);
			//BloggingContextFactory.ConfigureDBKind(bc, configuration, null);

			using (var db = new BloggingContext(/*bc.Options*/dbContextOptions))
			{
				db.Database.SetCommandTimeout(180);//long long running. timeouts prevention
												   //in sqlite only serializable - https://sqlite.org/isolation.html
				var isolation_level = db.ConnectionTypeName == "sqliteconnection" ? IsolationLevel.Serializable : IsolationLevel.ReadUncommitted;
				using (var trans = await db.Database.BeginTransactionAsync(isolation_level))//needed, other web nodes will read saved-caculating-state and exit thread
				{
					try
					{
						if (GetHashesInfoFromDB(db).Result != null)
						{
							logger.LogInformation(0, $"###Leaving calculation of initial Hash parameters; already present");
							return GetHashesInfoFromDB(db).Result;
						}
						logger.LogInformation(0, $"###Starting calculation of initial Hash parameters");

						hi = new HashesInfo { ID = 0, IsCalculating = true };

						await db.HashesInfo.AddAsync(hi);
						await db.SaveChangesAsync(true);
						_hashesInfoStatic = hi;//temporary save to static to indicate calculation and block new calcultion threads

						var alphabet = (from h in db.ThinHashes
										select h.Key.First()
										).Distinct()
										.OrderBy(o => o);
						var count = db.ThinHashes.CountAsync();
						var key_length = db.ThinHashes.MaxAsync(x => x.Key.Length);

						hi.Count = await count;
						hi.KeyLength = await key_length;
						hi.Alphabet = string.Concat(alphabet);
						hi.IsCalculating = false;

						db.Update(hi);
						await db.SaveChangesAsync(true);

						trans.Commit();
						logger.LogInformation(0, $"###Calculation of initial Hash parameters ended");
					}
					catch (Exception ex)
					{
						trans.Rollback();
						logger.LogError(ex, nameof(CalculateHashesInfo));
						hi = null;
					}
					finally
					{
						_hashesInfoStatic = hi;
					}
					return hi;
				}
			}
		}

		public async Task<ThinHashes> SearchAsync(HashInput hi)
		{
			ThinHashes found;
			switch (_entities.ConnectionTypeName)
			{
				case "sqliteconnection":
				case "mysqlconnection":
				case "sqlconnection":
					//case "npsqlconnection":
					if (hi.Kind == KindEnum.MD5)
					{
						found = await (from x in _entities.ThinHashes
									   where x.HashMD5 == hi.Search
									   select x)
									   .FirstOrDefaultAsync();
					}
					else
					{
						found = await (from x in _entities.ThinHashes
									   where x.HashSHA256 == hi.Search
									   select x)
									   .FirstOrDefaultAsync();
					}
					found = found ?? new ThinHashes { Key = _NOTHING_FOUND_TEXT };
					break;

				case "npsqlconnection":
					var search = new NpgsqlParameter("search", NpgsqlDbType.Char)
					{
						Value = hi.Search
					};
					if (hi.Kind == KindEnum.MD5)
					{
						search.Size = 32;
						found = await _entities.ThinHashes.FromSql("SELECT h.* FROM \"Hashes\" h WHERE h.\"hashMD5\" = @search", search)
							.FirstOrDefaultAsync()
							?? new ThinHashes { Key = _NOTHING_FOUND_TEXT };
					}
					else
					{
						search.Size = 64;
						found = await _entities.ThinHashes.FromSql("SELECT h.* FROM \"Hashes\" h WHERE h.\"hashSHA256\" = @search", search)
							.FirstOrDefaultAsync()
							?? new ThinHashes { Key = _NOTHING_FOUND_TEXT };
					}
					break;

				default:
					throw new NotSupportedException($"Bad {nameof(BloggingContext.ConnectionTypeName)} name");
			}

			return found;
		}
	}
}
