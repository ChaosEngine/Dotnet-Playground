#if INCLUDE_MONGODB

using DotnetPlayground.Models;
using Lib.AspNetCore.ServerTiming;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetPlayground.Repositories.Mongo
{
	public class MongoDBRepository : IHashesRepositoryPure
	{
		private const string _NOTHING_FOUND_TEXT = "nothing found";
		/// <summary>
		/// Used value or this specific worker node/process or load balancing server
		/// </summary>
		//private static HashesInfo _hashesInfoStatic;
		public static TimeSpan HashesInfoExpirationInMinutes = TimeSpan.FromHours(1);

		private readonly IMongoDatabase _db;
		private readonly IMongoCollection<ThinHashes> _hashesCol;
		private readonly ILogger<MongoDBRepository> _logger;
		private readonly IServerTiming _serverTiming;
		private readonly IMemoryCache _memoryCache;
		private HashesInfo _hi;
		public Stopwatch Watch { get; private set; }

		public MongoDBRepository(MongoService mongo, IMongoDBSettings settings, IMemoryCache memoryCache,
			ILogger<MongoDBRepository> logger, IServerTiming serverTiming)
		{
			_logger = logger;
			_serverTiming = serverTiming;
			_memoryCache = memoryCache;
			_db = mongo.Client.GetDatabase(settings.DatabaseName);
			_hashesCol = _db.GetCollection<ThinHashes>(settings.CollectionName);

			Watch = new Stopwatch();
			Watch.Start();
		}

		public Task<HashesInfo> CurrentHashesInfo
		{
			get { return GetHashesInfoFromDB(); }
		}

		/// <summary>
		/// Gets the hashes information from database.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="token">The token.</param>
		/// <returns></returns>
		private async Task<HashesInfo> GetHashesInfoFromDB(CancellationToken token = default)
		{
			if (!_memoryCache.TryGetValue<HashesInfo>(nameof(HashesInfo), out var hashesInfoStatic))
			{
				if (_hi == null)            //local value is empty, fill it from DB once
					_hi = await (await _db.GetCollection<HashesInfo>(nameof(HashesInfo)).FindAsync(x => x.ID == 0, null, token))
						.FirstOrDefaultAsync(token);

				if (_hi == null || _hi.IsCalculating)
					return _hi;             //still calculating, return just this local value
				else
				{
					//calculation ended, save to global static value
					hashesInfoStatic = await _memoryCache.GetOrCreateAsync(nameof(HashesInfo), (ce) =>
					{
						ce.SetAbsoluteExpiration(HashesInfoExpirationInMinutes);
						return Task.FromResult(_hi);
					});
				}
			}
			return hashesInfoStatic;
		}

		public async Task<IEnumerable<ThinHashes>> AutoComplete(string text)
		{
			_serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("ctor", Watch.ElapsedMilliseconds,
				"from ctor till AutoComplete"));

			text = text.Trim().ToLower();
			Task<List<ThinHashes>> found = _hashesCol.Find(p =>
				p.HashMD5.ToLower().StartsWith(text) ||
				p.HashSHA256.ToLower().StartsWith(text))
				.Limit(20).ToListAsync();

			_serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("READY",
				Watch.ElapsedMilliseconds, "AutoComplete ready"));
			return (await found).DefaultIfEmpty(new ThinHashes { Key = _NOTHING_FOUND_TEXT });
		}

		public async Task<HashesInfo> CalculateHashesInfo(ILogger logger, DbContextOptions<BloggingContext> dbContextOptions, CancellationToken token = default)
		{
			HashesInfo hi = null;

			try
			{
				if ((await GetHashesInfoFromDB(token)) != null)
				{
					logger.LogInformation(0, $"###Leaving calculation of initial Hash parameters; already present");
					hi = await GetHashesInfoFromDB(token);
					return hi;
				}
				logger.LogInformation(0, $"###Starting calculation of initial Hash parameters");

				hi = new HashesInfo { ID = 0, IsCalculating = true };

				var hi_col = _db.GetCollection<HashesInfo>(nameof(HashesInfo));
				await hi_col.InsertOneAsync(hi, null, token);
				//temporary save to static to indicate calculation and block new calcultion threads
				//_hashesInfoStatic = hi;
				await _memoryCache.GetOrCreateAsync(nameof(HashesInfo), (ce) =>
				{
					ce.SetAbsoluteExpiration(HashesInfoExpirationInMinutes.Multiply(2));
					return Task.FromResult(hi);
				});

				var alphabet = (await _hashesCol.Aggregate()
					.Group(selector => selector.Key.Substring(0, 1), group => new { FirstLetta = group.Key })
					.SortBy(s => s.FirstLetta)
					.ToListAsync())
					.Select(s => s.FirstLetta);

				//TODO: this or count* other method?
				var count = await _hashesCol.CountDocumentsAsync(new BsonDocument());
				var key_length = 0;
				if (count > 0)
				{
					var result = _hashesCol.Aggregate()
						.Project(x => new { Length = x.Key.Length })
						.SortByDescending(x => x.Length)
						.Limit(1);
					key_length = (await result.FirstOrDefaultAsync(token)).Length;
				}

				hi.Count = (int)count;
				hi.KeyLength = key_length;
				hi.Alphabet = string.Concat(alphabet);
				hi.IsCalculating = false;


				hi = await hi_col.FindOneAndReplaceAsync(
					Builders<HashesInfo>.Filter.Eq(p => p.ID, 0),
					hi,
					new FindOneAndReplaceOptions<HashesInfo> { ReturnDocument = ReturnDocument.After });

				logger.LogInformation(0, $"###Calculation of initial Hash parameters ended");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, nameof(CalculateHashesInfo));
				hi = null;
			}
			finally
			{
				_memoryCache.Set(nameof(HashesInfo), hi, HashesInfoExpirationInMinutes);
			}
			return hi;
		}

		public async Task<(IEnumerable<ThinHashes> Itemz, int Count)> PagedSearchAsync(string sortColumn, string sortOrderDirection,
			string searchText, int offset, int limit, CancellationToken token)
		{
			var AllColumnNames = Controllers.BaseController<ThinHashes>.AllColumnNames;

			if (!string.IsNullOrEmpty(sortColumn) && !AllColumnNames.Contains(sortColumn))
			{
				throw new ArgumentException("bad sort column");
			}
			else if (!string.IsNullOrEmpty(sortOrderDirection) &&
				sortOrderDirection != "asc" && sortOrderDirection != "desc")
			{
				throw new ArgumentException("bad sort direction");
			}

			try
			{
				_serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("ctor", Watch.ElapsedMilliseconds,
					"from ctor till PagedSearchAsync"));

				FilterDefinition<ThinHashes> filterDefinition;
				if (!string.IsNullOrEmpty(searchText))
				{
					searchText = $"/^{searchText}/s";

					var filterBuilder = Builders<ThinHashes>.Filter;
					BsonRegularExpression bsonregex = new BsonRegularExpression(searchText);
					filterDefinition =
						filterBuilder.Regex(x => x.Key, bsonregex) |
						filterBuilder.Regex(x => x.HashMD5, bsonregex) |
						filterBuilder.Regex(x => x.HashSHA256, bsonregex);
				}
				else
					filterDefinition = Builders<ThinHashes>.Filter.Empty;

				IFindFluent<ThinHashes, ThinHashes> source;
				if (!string.IsNullOrEmpty(sortColumn))
				{
					SortDefinition<ThinHashes> sortDefinition;
					bool descending = sortOrderDirection.EndsWith("desc", StringComparison.InvariantCultureIgnoreCase);
					if (descending)
						sortDefinition = Builders<ThinHashes>.Sort.Descending(sortColumn);
					else
						sortDefinition = Builders<ThinHashes>.Sort.Ascending(sortColumn);

					source = _hashesCol.Find(filterDefinition).Sort(sortDefinition);
				}
				else
					source = _hashesCol.Find(filterDefinition);

				var count = source.CountDocumentsAsync(token);
				var found = source.Skip(offset).Limit(limit).ToListAsync(token);
				await Task.WhenAll(count, found);

				return (found.Result, (int)count.Result);
			}
			finally
			{
				_serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("READY",
					Watch.ElapsedMilliseconds, $"PagedSearchMongoAsync ready"));
			}
		}

		public async Task<ThinHashes> SearchAsync(HashInput input)
		{
			_serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("ctor",
				Watch.ElapsedMilliseconds, "from ctor till SearchAsync"));

			ThinHashes found;
			if (input.Kind == KindEnum.MD5)
			{
				found = await _hashesCol.Find(p => p.HashMD5 == input.Search)
					.FirstOrDefaultAsync();
			}
			else
			{
				found = await _hashesCol.Find(p => p.HashSHA256 == input.Search)
					.FirstOrDefaultAsync();
			}
			found ??= new ThinHashes { Key = _NOTHING_FOUND_TEXT };

			_serverTiming.Metrics.Add(new Lib.AspNetCore.ServerTiming.Http.Headers.ServerTimingMetric("READY",
						Watch.ElapsedMilliseconds, "SearchAsync ready"));
			return found;
		}

		public void SetReadOnly(bool value)
		{
			//_hashesCol.Settings.ReadConcern = ReadConcern.Linearizable;
		}
	}
}

#endif