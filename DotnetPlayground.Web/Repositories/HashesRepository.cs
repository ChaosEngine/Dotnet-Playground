using DotnetPlayground.Services;
using DotnetPlayground;
using DotnetPlayground.Models;
using Lib.ServerTiming;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if INCLUDE_MYSQL
using MySqlConnector;
#endif
#if INCLUDE_POSTGRES
using Npgsql;
using NpgsqlTypes;
#endif
#if INCLUDE_ORACLE
using Oracle.ManagedDataAccess.Client;
#endif
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetPlayground.Repositories
{
	public interface IHashesRepositoryPure
	{
		Task<HashesInfo> CurrentHashesInfo { get; }

		void SetReadOnly(bool value);

		Task<IEnumerable<ThinHashes>> AutoComplete(string text);

		Task<(IEnumerable<ThinHashes> Itemz, int Count)> PagedSearchAsync(string sortColumn, string sortOrderDirection, string searchText,
			int offset, int limit, CancellationToken token);

		Task<HashesInfo> CalculateHashesInfo(ILogger logger, DbContextOptions<BloggingContext> dbContextOptions,
			CancellationToken token = default);

		Task<ThinHashes> SearchAsync(HashInput input);
	}

	public interface IHashesRepository : IGenericRepository<BloggingContext, ThinHashes>, IHashesRepositoryPure
	{
	}

	/// <summary>
	/// 
	/// </summary>
	/// <seealso cref="DotnetPlayground.Repositories.GenericRepository{DotnetPlayground.Models.BloggingContext, DotnetPlayground.Models.ThinHashes}" />
	/// <seealso cref="DotnetPlayground.Repositories.IHashesRepository" />
	public class HashesRepository : GenericRepository<BloggingContext, ThinHashes>, IHashesRepository
	{
		private const string _NOTHING_FOUND_TEXT = "nothing found";
		private static IEnumerable<string> _postgresAllColumnNames;
		/// <summary>
		/// Used value or this specific worker node/process or load balancing server
		/// </summary>
		//private static HashesInfo _hashesInfoStatic;
		public static TimeSpan HashesInfoExpirationInMinutes = TimeSpan.FromHours(1);
		/// <summary>
		/// locally cached value for request, refreshed upon every request.
		/// </summary>
		private HashesInfo _hi;
		private readonly IConfiguration _configuration;
		private readonly IMemoryCache _memoryCache;
		private readonly ILogger<HashesRepository> _logger;
		private readonly IServerTiming _serverTiming;
		public Stopwatch Watch { get; private set; }

		private static IEnumerable<String> PostgresAllColumnNames
		{
			get
			{
				if (_postgresAllColumnNames == null)
					_postgresAllColumnNames = AllColumnNames.Select(x => x.Replace("Hash", "hash"));
				return _postgresAllColumnNames;
			}
		}

		public Task<HashesInfo> CurrentHashesInfo
		{
			get { return GetHashesInfoFromDB(_entities); }
		}

		/// <summary>
		/// Gets the hashes information from database.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="token">The token.</param>
		/// <returns></returns>
		private async Task<HashesInfo> GetHashesInfoFromDB(BloggingContext db, CancellationToken token = default)
		{
			//if (_hashesInfoStatic == null)
			if (!_memoryCache.TryGetValue<HashesInfo>(nameof(HashesInfo), out var hashesInfoStatic))
			{
				if (_hi == null)            //local value is empty, fill it from DB once
					_hi = await db.HashesInfo.FirstOrDefaultAsync(x => x.ID == 0, token);

				if (_hi == null || _hi.IsCalculating)
					return _hi;             //still calculating, return just this local value
				else
				{
					//calculation ended, save to global static value
					//_hashesInfoStatic = _hi;
					hashesInfoStatic = await _memoryCache.GetOrCreateAsync(nameof(HashesInfo), (ce) =>
					{
						ce.SetAbsoluteExpiration(HashesInfoExpirationInMinutes);
						return Task.FromResult(_hi);
					});
				}
			}
			return hashesInfoStatic;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HashesRepository" /> class.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="configuration">The configuration.</param>
		public HashesRepository(BloggingContext context, IConfiguration configuration, IMemoryCache memoryCache,
			ILogger<HashesRepository> logger, IServerTiming serverTiming) : base(context)
		{
			_configuration = configuration;
			_memoryCache = memoryCache;
			_logger = logger;
			_serverTiming = serverTiming;

			Watch = new Stopwatch();
			Watch.Start();
		}

		public void SetReadOnly(bool value)
		{
			_entities.ChangeTracker.QueryTrackingBehavior = value ? QueryTrackingBehavior.NoTracking : QueryTrackingBehavior.TrackAll;
		}

		/// <summary>
		/// Automatics the complete.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns></returns>
		public async Task<IEnumerable<ThinHashes>> AutoComplete(string text)
		{
			_serverTiming.Metrics.Add(new Lib.ServerTiming.Http.Headers.ServerTimingMetric("ctor", Watch.ElapsedMilliseconds,
				"from ctor till AutoComplete"));

			text = $"{text.Trim().ToLower()}%";
			Task<List<ThinHashes>> found = null;

			switch (_entities.ConnectionTypeName)
			{
				case "sqliteconnection":
				case "mysqlconnection":
				case "sqlconnection":
				case "npsqlconnection":
				case "oracleconnection":
					found = (from x in _entities.ThinHashes
							 where EF.Functions.Like(x.HashMD5, text) || EF.Functions.Like(x.HashSHA256, text)
							 select x)
						.Take(20)
						//.DefaultIfEmpty(new ThinHashes { Key = _NOTHING_FOUND_TEXT })
						.ToListAsync();
					break;

				/*case "sqlconnection":
+					found = _entities.ThinHashes.FromSql(
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

			_serverTiming.Metrics.Add(new Lib.ServerTiming.Http.Headers.ServerTimingMetric("READY",
				Watch.ElapsedMilliseconds, "AutoComplete ready"));
			return (await found).DefaultIfEmpty(new ThinHashes { Key = _NOTHING_FOUND_TEXT });
		}

		/// <summary>
		/// creating Sql WHERE statment Column conditions.
		/// </summary>
		/// <param name="colNamePrefix">The col name prefix.</param>
		/// <param name="colNameSuffix">The col name suffix.</param>
		/// <param name="columnNames">The column names.</param>
		/// <param name="searchTextParamName">Name of the search text parameter.</param>
		/// <returns></returns>
		private string WhereColumnCondition(char colNamePrefix, char colNameSuffix, IEnumerable<string> columnNames = null,
			string searchTextParamName = "searchText", char paramPrefix = '@')
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
				sb.AppendFormat("{0}({3}{1}{4} LIKE {6}{2}_{5})", comma, col, searchTextParamName, colNamePrefix, colNameSuffix, col,
					paramPrefix);
				comma = " OR" + Environment.NewLine + '\t';
			}
			sb.Append(@"
)
");
			return sb.ToString();
		}

#if INCLUDE_SQLSERVER
		/// <summary>
		/// Searches the SQL server asynchronous.
		/// </summary>
		/// <param name="sortColumn">The sort column.</param>
		/// <param name="sortOrderDirection">The sort order direction.</param>
		/// <param name="searchText">The search text.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="limit">The limit.</param>
		/// <param name="token">The token.</param>
		/// <returns></returns>
		private async Task<(IEnumerable<ThinHashes> Itemz, int Count)> PagedSearchSqlServerAsync(string sortColumn, string sortOrderDirection,
					string searchText, int offset, int limit, CancellationToken token)
		{
			//string col_names = string.Join("],[", AllColumnNames);
			string sql =
(string.IsNullOrEmpty(searchText) ?
$@"SELECT A.*,
	(SELECT rows FROM sysindexes WHERE id = OBJECT_ID('Hashes') AND indid < 2) cnt
FROM 
(
    SELECT *
    FROM [Hashes]
	{(string.IsNullOrEmpty(sortColumn) ? "ORDER BY 1" : $"ORDER BY [{sortColumn}] {sortOrderDirection}")}
    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY
) A
"
:
$@"
WITH RowAndWhere AS
(
    SELECT *
    FROM [Hashes]
    WHERE {WhereColumnCondition('[', ']')}
)
SELECT RaW.*, (SELECT COUNT(*) FROM RowAndWhere) cnt
FROM RowAndWhere RaW
{(string.IsNullOrEmpty(sortColumn) ? "ORDER BY 1" : $"ORDER BY [{sortColumn}] {sortOrderDirection}")}
OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY
");

			var conn = _entities.Database.GetDbConnection();
			try
			{
				var found = new List<ThinHashes>(limit);
				int count = 0;

				await conn.OpenAsync(token);
				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = sql;
					_logger.LogInformation("sql => {0}", sql);
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
						parameter.ParameterName = $"@searchText_{nameof(ThinHashes.Key)}";
						parameter.DbType = DbType.String;
						parameter.Size = 20;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);

						parameter = cmd.CreateParameter();
						parameter.ParameterName = $"@searchText_{nameof(ThinHashes.HashMD5)}";
						parameter.DbType = DbType.String;
						parameter.Size = 32;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);

						parameter = cmd.CreateParameter();
						parameter.ParameterName = $"@searchText_{nameof(ThinHashes.HashSHA256)}";
						parameter.DbType = DbType.String;
						parameter.Size = 64;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);
					}

					using (var rdr = await cmd.ExecuteReaderAsync(token))
					{
						object[] strings = new object[4];
						while (await rdr.ReadAsync(token))
						{
							rdr.GetValues(strings);
							found.Add(
							new ThinHashes
							{
								Key = (string)strings[0],
								HashMD5 = (string)strings[1],
								HashSHA256 = (string)strings[2],
							}
							);
						}
						if (strings[3] != null)
							count = int.Parse(strings[3].ToString());
					}
				}

				return (found, count);
			}
			finally
			{
				conn.Close();
			}
		}
#endif

#if INCLUDE_MYSQL
		/// <summary>
		/// Searches my SQL asynchronous.
		/// </summary>
		/// <param name="sortColumn">The sort column.</param>
		/// <param name="sortOrderDirection">The sort order direction.</param>
		/// <param name="searchText">The search text.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="limit">The limit.</param>
		/// <param name="token">The token.</param>
		/// <returns></returns>
		private async Task<(List<ThinHashes> Itemz, int Count)> PagedSearchMySqlAsync(string sortColumn, string sortOrderDirection, string searchText,
					int offset, int limit, CancellationToken token)
		{
			//string col_names = string.Join("`,`", AllColumnNames);
			string sql = "SET SESSION SQL_BIG_SELECTS=1;" +
(string.IsNullOrEmpty(searchText) ?
$@"
SELECT
	A.*, (SELECT COUNT(*) FROM `Hashes`) cnt
FROM 
(
	SELECT *
	FROM `Hashes`
		--The 'deferred join.' approach taken from https://aaronfrancis.com/2022/efficient-pagination-using-deferred-joins
		inner join (                	-- The 'deferred join.'
			select `Key` from `Hashes`  -- The pagination using a fast index.
			order by `Key` 
			LIMIT @limit OFFSET @offset
		) as tmp using(`Key`)
	{(string.IsNullOrEmpty(sortColumn) ? "" : $"ORDER BY `{sortColumn}` {sortOrderDirection}")}

) A
"
:
$@"
WITH RowAndWhere AS
(
    SELECT *
    FROM `Hashes`
    WHERE {WhereColumnCondition('`', '`')}
)

SELECT RaW.*, (SELECT COUNT(*) FROM RowAndWhere) cnt
FROM RowAndWhere RaW
{(string.IsNullOrEmpty(sortColumn) ? "" : $"ORDER BY `{sortColumn}` {sortOrderDirection}")}
LIMIT @limit OFFSET @offset
");

			using (var conn = new MySqlConnection(_configuration.GetConnectionString("MySQL")))
			{
				var found = new List<ThinHashes>(limit);
				int count = 0;

				await conn.OpenAsync(token);
				using (var cmd = new MySqlCommand(sql, conn))
				{
					cmd.CommandText = sql;
					_logger.LogInformation("sql => {0}", sql);
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
						parameter.ParameterName = $"@searchText_{nameof(ThinHashes.Key)}";
						parameter.DbType = DbType.String;
						parameter.Size = 20;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);

						parameter = cmd.CreateParameter();
						parameter.ParameterName = $"@searchText_{nameof(ThinHashes.HashMD5)}";
						parameter.DbType = DbType.String;
						parameter.Size = 32;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);

						parameter = cmd.CreateParameter();
						parameter.ParameterName = $"@searchText_{nameof(ThinHashes.HashSHA256)}";
						parameter.DbType = DbType.String;
						parameter.Size = 64;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);
					}

					using (var rdr = await cmd.ExecuteReaderAsync(token))
					{
						object[] strings = new object[4];
						while (await rdr.ReadAsync(token))
						{
							rdr.GetValues(strings);
							found.Add(
							new ThinHashes
							{
								Key = (string)strings[0],
								HashMD5 = (string)strings[1],
								HashSHA256 = (string)strings[2],
							}
							);
						}
						if (strings[3] != null)
							count = int.Parse(strings[3].ToString());
					}
				}

				return (found, count);
			}//end using
		}
#endif

		/// <summary>
		/// Searches the sqlite asynchronous.
		/// </summary>
		/// <param name="sortColumn">The sort column.</param>
		/// <param name="sortOrderDirection">The sort order direction.</param>
		/// <param name="searchText">The search text.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="limit">The limit.</param>
		/// <param name="token">The token.</param>
		/// <returns></returns>
		private async Task<(IEnumerable<ThinHashes> Itemz, int Count)> PagedSearchSqliteAsync(string sortColumn, string sortOrderDirection,
					string searchText, int offset, int limit, CancellationToken token)
		{
			string col_names = string.Join("],[", AllColumnNames);
			string sql;

			if (string.IsNullOrEmpty(searchText))
			{
				sql = $@"SELECT count(*) cnt FROM Hashes
;
SELECT [{col_names}]
FROM Hashes
	--The 'deferred join.' approach taken from https://aaronfrancis.com/2022/efficient-pagination-using-deferred-joins
	inner join (                	-- The 'deferred join.'
		select [Key] from Hashes  -- The pagination using a fast index.
		LIMIT @limit OFFSET @offset
	) as tmp using([Key])

{(string.IsNullOrEmpty(sortColumn) ? "" : $"ORDER BY [{sortColumn}] {sortOrderDirection}")}
";
			}
			else
			{
				string temp_tab_name = $"tempo_{Guid.NewGuid():N}";
				sql = $@"
CREATE TEMPORARY TABLE {temp_tab_name} AS
SELECT [{col_names}]
FROM Hashes
WHERE {WhereColumnCondition('[', ']')}
;
SELECT count(*) cnt FROM {temp_tab_name}
;
SELECT [{col_names}]
FROM {temp_tab_name}

{(string.IsNullOrEmpty(sortColumn) ? "" : $"ORDER BY [{sortColumn}] {sortOrderDirection}")}
LIMIT @limit OFFSET @offset
";
			}

			var conn = _entities.Database.GetDbConnection();
			try
			{
				var found = new List<ThinHashes>(limit);
				int count = -1;

				await conn.OpenAsync(token);
				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandText = sql;
					_logger.LogInformation("sql => {0}", sql);
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
						parameter.ParameterName = $"@searchText_{nameof(ThinHashes.Key)}";
						parameter.DbType = DbType.String;
						parameter.Size = 20;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);

						parameter = cmd.CreateParameter();
						parameter.ParameterName = $"@searchText_{nameof(ThinHashes.HashMD5)}";
						parameter.DbType = DbType.String;
						parameter.Size = 32;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);

						parameter = cmd.CreateParameter();
						parameter.ParameterName = $"@searchText_{nameof(ThinHashes.HashSHA256)}";
						parameter.DbType = DbType.String;
						parameter.Size = 64;
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
							while (await rdr.ReadAsync(token))
							{
								string[] strings = new string[3];
								rdr.GetValues(strings);
								found.Add(new ThinHashes
								{
									Key = strings[0],
									HashMD5 = strings[1],
									HashSHA256 = strings[2]
								}/*strings*/);
							}
						}
						else
						{
							//found.Add(new ThinHashes { Key = _NOTHING_FOUND_TEXT });
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

#if INCLUDE_POSTGRES
		/// <summary>
		/// Searches the postgres asynchronous.
		/// </summary>
		/// <param name="sortColumn">The sort column.</param>
		/// <param name="sortOrderDirection">The sort order direction.</param>
		/// <param name="searchText">The search text.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="limit">The limit.</param>
		/// <param name="token">The token.</param>
		/// <returns></returns>
		private async Task<(IEnumerable<ThinHashes> Itemz, int Count)> PagedSearchPostgresAsync(string sortColumn, string sortOrderDirection, string searchText, int offset, int limit, CancellationToken token)
		{
			// string col_names = string.Join("\",\"", PostgresAllColumnNames);
			string sql =
(string.IsNullOrEmpty(searchText) ?
$@"
SELECT A.*, (SELECT count(*) FROM ""Hashes"") cnt
FROM 
(SELECT *
FROM ""Hashes""
	--The 'deferred join.' approach taken from https://aaronfrancis.com/2022/efficient-pagination-using-deferred-joins
	inner join (                	-- The 'deferred join.'
		select ""Key"" from ""Hashes""  -- The pagination using a fast index.
		order by ""Key"" 
		LIMIT @limit OFFSET @offset
	) as tmp using(""Key"")
{(string.IsNullOrEmpty(sortColumn) ? "" : $"ORDER BY \"{PostgresAllColumnNames.FirstOrDefault(x => string.Compare(x, sortColumn, StringComparison.CurrentCultureIgnoreCase) == 0)}\" {sortOrderDirection}")}
) A
"
:
$@"
WITH RowAndWhere AS
(
	SELECT *
	FROM ""Hashes""
    WHERE {WhereColumnCondition('"', '"', PostgresAllColumnNames)}
)
SELECT B.*, (SELECT COUNT(*) FROM RowAndWhere) cnt
FROM RowAndWhere B
{(string.IsNullOrEmpty(sortColumn) ? "" : $"ORDER BY B.\"{PostgresAllColumnNames.FirstOrDefault(x => string.Compare(x, sortColumn, StringComparison.CurrentCultureIgnoreCase) == 0)}\" {sortOrderDirection}")}
LIMIT @limit OFFSET @offset
");

			using (var conn = new NpgsqlConnection(_configuration.GetConnectionString("PostgreSql")))
			{
				conn.ProvideClientCertificatesCallback = ContextFactory.MyProvideClientCertificatesCallback;

				var found = new List<ThinHashes>(limit);
				int count = 0;

				await conn.OpenAsync(token);
				using (var cmd = new NpgsqlCommand(sql, conn))
				{
					cmd.CommandText = sql;
					_logger.LogInformation("sql => {0}", sql);
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
						parameter.ParameterName = $"@searchText_{nameof(ThinHashes.Key)}";
						parameter.DbType = DbType.String;
						parameter.Size = 20;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);

						parameter = cmd.CreateParameter();
						parameter.ParameterName = $"@searchText_{nameof(ThinHashes.HashMD5)}";
						parameter.DbType = DbType.String;
						parameter.Size = 32;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);

						parameter = cmd.CreateParameter();
						parameter.ParameterName = $"@searchText_{nameof(ThinHashes.HashSHA256)}";
						parameter.DbType = DbType.String;
						parameter.Size = 64;
						parameter.Value =  /*'%' + */searchText + '%';
						cmd.Parameters.Add(parameter);
					}

					using (var rdr = await cmd.ExecuteReaderAsync(token))
					{
						object[] strings = new object[4];
						while (await rdr.ReadAsync(token))
						{
							rdr.GetValues(strings);
							found.Add(
							new ThinHashes
							{
								Key = (string)strings[0],
								HashMD5 = (string)strings[1],
								HashSHA256 = (string)strings[2],
							}
							);
						}
						if (strings[3] != null)
							count = int.Parse(strings[3].ToString());
					}
				}

				return (found, count);
			}//end using
		}
#endif

#if INCLUDE_ORACLE
		/// <summary>
		/// Searches the oracle asynchronous.
		/// </summary>
		/// <param name="sortColumn">The sort column.</param>
		/// <param name="sortOrderDirection">The sort order direction.</param>
		/// <param name="searchText">The search text.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="limit">The limit.</param>
		/// <param name="token">The token.</param>
		/// <returns></returns>
		private async Task<(IEnumerable<ThinHashes> Itemz, int Count)> PagedSearchOracleAsync(
			string sortColumn, string sortOrderDirection, string searchText, int offset, int limit, CancellationToken token)
		{
			string sql =
(string.IsNullOrEmpty(searchText) ?
$@"
SELECT A.*, (select num_rows from user_tables where table_name = 'Hashes') cnt
FROM 
(SELECT /*+ FIRST_ROWS({limit}) */ *
FROM ""Hashes""
	--The 'deferred join.' approach taken from https://aaronfrancis.com/2022/efficient-pagination-using-deferred-joins
	inner join (                	-- The 'deferred join.'
		select ""Key"" from ""Hashes""  -- The pagination using a fast index.
		order by ""Key"" 
		OFFSET :offset ROWS FETCH NEXT :limit ROWS ONLY
	) tmp using(""Key"")
{(string.IsNullOrEmpty(sortColumn) ? "" : $"ORDER BY \"{PostgresAllColumnNames.FirstOrDefault(x => string.Compare(x, sortColumn, StringComparison.CurrentCultureIgnoreCase) == 0)}\" {sortOrderDirection}")}
) A
"
:
$@"
WITH RowAndWhere AS
(
    SELECT A.*
    FROM (SELECT /*+ FIRST_ROWS({limit}) */ *
		  FROM ""Hashes""
          WHERE {WhereColumnCondition(colNamePrefix: '"', colNameSuffix: '"', columnNames: PostgresAllColumnNames, paramPrefix: ':')}
         ) A
)
SELECT WhereAndOrder.* FROM (
  SELECT B.*, (SELECT COUNT(*) FROM RowAndWhere) cnt
  FROM RowAndWhere B
  {(string.IsNullOrEmpty(sortColumn) ? "" : $"ORDER BY B.\"{PostgresAllColumnNames.FirstOrDefault(x => string.Compare(x, sortColumn, StringComparison.CurrentCultureIgnoreCase) == 0)}\" {sortOrderDirection}")}
) WhereAndOrder
OFFSET :offset ROWS FETCH NEXT :limit ROWS ONLY
");
			using (var conn = new OracleConnection(_configuration.GetConnectionString("Oracle")))
			{
				var found = new List<ThinHashes>(limit);
				int count = 0;

				await conn.OpenAsync(token);
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.CommandText = sql;
					_logger.LogInformation("sql => {0}", sql);
					cmd.CommandTimeout = 240;
					DbParameter parameter;

					parameter = cmd.CreateParameter();
					parameter.ParameterName = ":offset";
					parameter.DbType = DbType.Int32;
					parameter.Value = offset;
					cmd.Parameters.Add(parameter);

					parameter = cmd.CreateParameter();
					parameter.ParameterName = ":limit";
					parameter.DbType = DbType.Int32;
					parameter.Value = limit;
					cmd.Parameters.Add(parameter);

					if (!string.IsNullOrEmpty(searchText))
					{
						parameter = cmd.CreateParameter();
						parameter.ParameterName = $":searchText_{nameof(ThinHashes.Key)}";
						parameter.DbType = DbType.String;
						parameter.Size = 20;
						parameter.Value = searchText + '%';
						cmd.Parameters.Add(parameter);

						parameter = cmd.CreateParameter();
						parameter.ParameterName = $":searchText_{nameof(ThinHashes.HashMD5)}";
						parameter.DbType = DbType.String;
						parameter.Size = 32;
						parameter.Value = searchText + '%';
						cmd.Parameters.Add(parameter);

						parameter = cmd.CreateParameter();
						parameter.ParameterName = $":searchText_{nameof(ThinHashes.HashSHA256)}";
						parameter.DbType = DbType.String;
						parameter.Size = 64;
						parameter.Value = searchText + '%';
						cmd.Parameters.Add(parameter);
					}

					using (var rdr = await cmd.ExecuteReaderAsync(token))
					{
						object[] strings = new object[4];
						while (await rdr.ReadAsync(token))
						{
							rdr.GetValues(strings);
							found.Add(
							new ThinHashes
							{
								Key = (string)strings[0],
								HashMD5 = (string)strings[1],
								HashSHA256 = (string)strings[2],
							}
							);
						}
						if (strings[3] != null)
							count = int.Parse(strings[3].ToString());
					}
				}

				return (found, count);
			}//end using
		}
#endif

		/// <summary>
		/// Searches the asynchronous.
		/// </summary>
		/// <param name="sortColumn">The sort column.</param>
		/// <param name="sortOrderDirection">The sort order direction.</param>
		/// <param name="searchText">The search text.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="limit">The limit.</param>
		/// <param name="token">The token.</param>
		/// <returns></returns>
		public async Task<(IEnumerable<ThinHashes> Itemz, int Count)> PagedSearchAsync(string sortColumn, string sortOrderDirection, string searchText,
			int offset, int limit, CancellationToken token)
		{
			_serverTiming.Metrics.Add(new Lib.ServerTiming.Http.Headers.ServerTimingMetric("ctor", Watch.ElapsedMilliseconds,
				"from ctor till PagedSearchAsync"));

			if (!string.IsNullOrEmpty(sortColumn) && !AllColumnNames.Contains(sortColumn))
			{
				throw new ArgumentException("bad sort column");
			}
			else if (!string.IsNullOrEmpty(sortOrderDirection) &&
				sortOrderDirection != "asc" && sortOrderDirection != "desc")
			{
				throw new ArgumentException("bad sort direction");
			}

			if (string.IsNullOrEmpty(searchText) || searchText.Length > 2)
			{
				try
				{
					switch (_entities.ConnectionTypeName)
					{
#if INCLUDE_MYSQL
						case "mysqlconnection":
							return await PagedSearchMySqlAsync(sortColumn, sortOrderDirection, searchText, offset, limit, token);
#endif

#if INCLUDE_SQLSERVER
						case "sqlconnection":
							return await PagedSearchSqlServerAsync(sortColumn, sortOrderDirection, searchText, offset, limit, token);
#endif
						case "sqliteconnection":
							return await PagedSearchSqliteAsync(sortColumn, sortOrderDirection, searchText, offset, limit, token);
#if INCLUDE_POSTGRES
						case "npsqlconnection":
							return await PagedSearchPostgresAsync(sortColumn, sortOrderDirection, searchText, offset, limit, token);
#endif

#if INCLUDE_ORACLE
						case "oracleconnection":
							return await PagedSearchOracleAsync(sortColumn, sortOrderDirection, searchText, offset, limit, token);
#endif
						default:
							throw new NotSupportedException($"Bad {nameof(BloggingContext.ConnectionTypeName)} name");
					}
				}
				finally
				{
					_serverTiming.Metrics.Add(new Lib.ServerTiming.Http.Headers.ServerTimingMetric("READY",
						Watch.ElapsedMilliseconds, $"PagedSearch{_entities.ConnectionTypeName}Async ready"));
				}
			}
			else
			{
				var hashes = _entities.ThinHashes.AsNoTracking();
				_entities.Database.SetCommandTimeout(240);

				Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction trans = null;
				try
				{
					if (_entities.ConnectionTypeName == "mysqlconnection")
					{
						trans = await _entities.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, token);
						await _entities.Database.ExecuteSqlRawAsync("SET SESSION SQL_BIG_SELECTS=1;", token);
					}

					if (!string.IsNullOrEmpty(searchText))
					{
						//students = students.Where(s =>
						//	s.Key.StartsWith(searchText) || s.HashMD5.StartsWith(searchText) || s.HashSHA256.StartsWith(searchText));
						searchText = searchText + '%';
						hashes = hashes.Where(s =>
							EF.Functions.Like(s.Key, searchText) ||
							EF.Functions.Like(s.HashMD5, searchText) ||
							EF.Functions.Like(s.HashSHA256, searchText)
							);
					}

					if (!string.IsNullOrEmpty(sortColumn))
					{
						bool descending = sortOrderDirection.EndsWith("desc", StringComparison.InvariantCultureIgnoreCase);

						if (descending)
							hashes = hashes.OrderByDescending(s => EF.Property<ThinHashes>(s, sortColumn));
						else
							hashes = hashes.OrderBy(s => EF.Property<ThinHashes>(s, sortColumn));
					}
					var found = await PaginatedList<ThinHashes>.CreateAsync(hashes, offset, limit, token);

					_serverTiming.Metrics.Add(new Lib.ServerTiming.Http.Headers.ServerTimingMetric("READY",
						Watch.ElapsedMilliseconds, "PagedSearchAsync ready"));
					return (found, found.FoundCount);
				}
				finally
				{
					if (_entities.ConnectionTypeName == "mysqlconnection")
					{
						await trans.CommitAsync(token);
						trans.Dispose();
					}
				}
			}
		}

		/// <summary>
		/// Creates hashes info
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="dbContextOptions">The database context options.</param>
		/// <param name="token">The token.</param>
		/// <returns></returns>
		public async Task<HashesInfo> CalculateHashesInfo(ILogger logger, DbContextOptions<BloggingContext> dbContextOptions,
			CancellationToken token = default)
		{
			HashesInfo hi = null;

			using (var db = new BloggingContext(dbContextOptions))
			{
				db.Database.SetCommandTimeout(180);//long long running. timeouts prevention
												   //in sqlite only serializable - https://sqlite.org/isolation.html
				IsolationLevel isolation_level = new[] { "sqliteconnection", "oracleconnection" }.Contains(db.ConnectionTypeName) ?
					IsolationLevel.Serializable : IsolationLevel.ReadUncommitted;
				using (var trans = await db.Database.BeginTransactionAsync(isolation_level, token))//needed, other web nodes will read saved-caculating-state and exit thread
				{
					try
					{
						if (GetHashesInfoFromDB(db, token).Result != null)
						{
							logger.LogInformation("###Leaving calculation of initial Hash parameters; already present");
							hi = GetHashesInfoFromDB(db, token).Result;
							return hi;
						}
						logger.LogInformation("###Starting calculation of initial Hash parameters");

						hi = new HashesInfo { ID = 0, IsCalculating = true };

						if (db.ConnectionTypeName == "mysqlconnection")
							await db.Database.ExecuteSqlRawAsync("SET SQL_BIG_SELECTS=1", token);
						await db.HashesInfo.AddAsync(hi, token);
						await db.SaveChangesAsync(true, token);
						//temporary save to static to indicate calculation and block new calcultion threads
						//_hashesInfoStatic = hi;
						await _memoryCache.GetOrCreateAsync(nameof(HashesInfo), (ce) =>
						{
							ce.SetAbsoluteExpiration(HashesInfoExpirationInMinutes.Multiply(2));
							return Task.FromResult(hi);
						});

						var alphabet = (from h in db.ThinHashes
										select h.Key.FirstOrDefault()
										).Distinct()
										.OrderBy(o => o);
						var count = await db.ThinHashes.CountAsync(token);
						var key_length = 0;
						if (count > 0)
							key_length = await db.ThinHashes.MaxAsync(x => x.Key.Length, token);

						hi.Count = count;
						hi.KeyLength = key_length;
						hi.Alphabet = string.Concat(alphabet);
						hi.IsCalculating = false;

						db.Update(hi);
						await db.SaveChangesAsync(true, token);

						await trans.CommitAsync(token);
						logger.LogInformation("###Calculation of initial Hash parameters ended");
					}
					catch (Exception ex)
					{
						await trans.RollbackAsync(token);
						logger.LogError(ex, nameof(CalculateHashesInfo));
						hi = null;
					}
					finally
					{
						//_hashesInfoStatic = hi;
						_memoryCache.Set(nameof(HashesInfo), hi, HashesInfoExpirationInMinutes);
					}
					return hi;
				}
			}
		}

		/// <summary>
		/// Search in asynchronous way.
		/// </summary>
		/// <param name="hi">The hi.</param>
		/// <returns></returns>
		public async Task<ThinHashes> SearchAsync(HashInput hi)
		{
			_serverTiming.Metrics.Add(new Lib.ServerTiming.Http.Headers.ServerTimingMetric("ctor",
				Watch.ElapsedMilliseconds, "from ctor till SearchAsync"));

			ThinHashes found;
			switch (_entities.ConnectionTypeName)
			{
				case "sqliteconnection":
				case "mysqlconnection":
				case "sqlconnection":
				case "oracleconnection":
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

#if INCLUDE_POSTGRES
				case "npsqlconnection":
					var search = new NpgsqlParameter("search", NpgsqlDbType.Char)
					{
						Value = hi.Search
					};
					if (hi.Kind == KindEnum.MD5)
					{
						search.Size = 32;
						found = await _entities.ThinHashes.FromSqlRaw("SELECT h.* FROM \"Hashes\" h WHERE h.\"hashMD5\" = @search", search)
							.FirstOrDefaultAsync()
							?? new ThinHashes { Key = _NOTHING_FOUND_TEXT };
					}
					else
					{
						search.Size = 64;
						found = await _entities.ThinHashes.FromSqlRaw("SELECT h.* FROM \"Hashes\" h WHERE h.\"hashSHA256\" = @search", search)
							.FirstOrDefaultAsync()
							?? new ThinHashes { Key = _NOTHING_FOUND_TEXT };
					}
					break;
#endif

				default:
					throw new NotSupportedException($"Bad {nameof(BloggingContext.ConnectionTypeName)} name");
			}

			_serverTiming.Metrics.Add(new Lib.ServerTiming.Http.Headers.ServerTimingMetric("READY",
						Watch.ElapsedMilliseconds, "SearchAsync ready"));
			return found;
		}
	}

	/// <summary>
	/// Background hash claculation processing job
	/// </summary>
	/// <seealso cref="DotnetPlayground.Services.BackgroundOperationBase" />
	public sealed class CalculateHashesInfoBackgroundOperation : BackgroundOperationBase
	{
		/// <summary>
		/// Does the work.
		/// </summary>
		/// <param name="services">The services.</param>
		/// <param name="token">The token.</param>
		/// <returns></returns>
		public override async Task DoWorkAsync(IServiceProvider services, CancellationToken token)
		{
			using (var scope = services.CreateScope())
			{
				var repo = scope.ServiceProvider.GetRequiredService<IHashesRepositoryPure>();
				var dbContextOptions = scope.ServiceProvider.GetRequiredService<DbContextOptions<BloggingContext>>();
				var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
				var logger = loggerFactory.CreateLogger<CalculateHashesInfoBackgroundOperation>();

				var hi = await repo.CalculateHashesInfo(logger, dbContextOptions, token);
			}
		}
	}
}
