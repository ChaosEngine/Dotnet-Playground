using EFGetStarted.AspNetCore.ExistingDb.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.ExistingDb.Repositories
{
	sealed class DocumentDBHash : ThinHashes
	{
		/// <summary>
		/// Document DB enforces Id node to be (auto)generated
		/// </summary>
		[JsonProperty(PropertyName = "id")]
		public string Id { get; set; }
	}

	internal interface IThinHashesDocumentDBRepository : IHashesRepository
	{
		Task<IEnumerable<DocumentDBHash>> GetItemsSortedDescByKeyAsync(int itemsCount = -1);
		Task<DocumentDBHash> GetByIDAsync(string id);
		Task<IEnumerable<DocumentDBHash>> GetItemsAsync(Expression<Func<DocumentDBHash, bool>> predicate, int itemsCount = -1);
	}

	internal class ThinHashesDocumentDBRepository : DocumentDBRepository<DocumentDBHash>, IThinHashesDocumentDBRepository, IDisposable
	{
		private const string _NOTHING_FOUND_TEXT = "nothing found";
		private Guid _transactionId;//TODO: kill it

		//TODO: properly implement
		public Task<HashesInfo> CurrentHashesInfo => CalculateHashesInfo(null, null, null, null);

		public ThinHashesDocumentDBRepository(string endpoint, string key, string databaseId, string collectionId)
			: base(endpoint, key, databaseId, collectionId)
		{
			//TODO: kill it
			_transactionId = Guid.NewGuid();

			Initialize();
		}

		/// <summary>
		/// Initialize Document DB client access object
		/// </summary>
		protected override void Initialize()
		{
			_client = new DocumentClient(new Uri(_endpoint), _key);
			CreateDatabaseIfNotExistsAsync().Wait();
			CreateCollectionIfNotExistsAsync(new DocumentCollection
			{
				Id = _collectionId,
				//myCollection.PartitionKey.Paths.Add("/Key"),
				UniqueKeyPolicy = new UniqueKeyPolicy
				{
					UniqueKeys = new Collection<UniqueKey>
					{
						new UniqueKey { Paths = new Collection<string> { "/Key", "/HashMD5", "/HashSHA256" }},
					}
				},
				IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 }),
			}).Wait();
		}

		public async Task<IEnumerable<DocumentDBHash>> GetItemsSortedDescByKeyAsync(int itemsCount = -1)
		{
			IDocumentQuery<DocumentDBHash> query = _client.CreateDocumentQuery<DocumentDBHash>(
				UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
				new FeedOptions { MaxItemCount = itemsCount/*, EnableCrossPartitionQuery = true*/ })
				//.Where(predicate)
				.OrderByDescending(o => o.Key)
				.Take(itemsCount)
				.AsDocumentQuery();

			List<DocumentDBHash> results = new List<DocumentDBHash>();
			while (query.HasMoreResults)
			{
				results.AddRange(await query.ExecuteNextAsync<DocumentDBHash>());
			}

			return results;
		}

		internal async Task InvokeBulkDeleteSproc()
		{
			var client = new DocumentClient(new Uri(_endpoint), _key);
			Uri collectionLink = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId);

			string scriptName = "bulkDelete.js";

			Uri sprocUri = UriFactory.CreateStoredProcedureUri(_databaseId, _collectionId, scriptName);

			try
			{
				int count = 20;
				int deleted;
				bool continuation;
				do
				{
					var response = await client.ExecuteStoredProcedureAsync<Document>(sprocUri,
						//new RequestOptions { PartitionKey = new PartitionKey("mmmmm") },
						_transactionId);
					continuation = response.Response.GetPropertyValue<bool>("continuation");
					deleted = response.Response.GetPropertyValue<int>("deleted");
				}
				while (continuation && count-- > 0);
			}
			catch (DocumentClientException)
			{
				throw;
			}
		}

		internal async Task<int> InvokeBulkInsertSproc(List<DocumentDBHash> documents)
		{
			int maxFiles = 2000, maxScriptSize = 50000;
			int currentCount = 0;
			int fileCount = maxFiles != 0 ? Math.Min(maxFiles, documents.Count) : documents.Count;


			Uri sproc = UriFactory.CreateStoredProcedureUri(_databaseId, _collectionId, "bulkImport.js");


			// 4. Create a batch of docs (MAX is limited by request size (2M) and to script for execution.           
			// We send batches of documents to create to script.
			// Each batch size is determined by MaxScriptSize.
			// MaxScriptSize should be so that:
			// -- it fits into one request (MAX reqest size is 16Kb).
			// -- it doesn't cause the script to time out.
			// -- it is possible to experiment with MaxScriptSize to get best perf given number of throttles, etc.
			while (currentCount < fileCount)
			{
				// 5. Create args for current batch.
				//    Note that we could send a string with serialized JSON and JSON.parse it on the script side,
				//    but that would cause script to run longer. Since script has timeout, unload the script as much
				//    as we can and do the parsing by client and framework. The script will get JavaScript objects.
				string argsJson = CreateBulkInsertScriptArguments(documents, currentCount, fileCount, maxScriptSize);

				var args = new dynamic[] { JsonConvert.DeserializeObject<dynamic>(argsJson) };

				// 6. execute the batch.
				StoredProcedureResponse<int> scriptResult = await _client.ExecuteStoredProcedureAsync<int>(
					sproc,
					//new RequestOptions { PartitionKey = new PartitionKey("mmmmm") },
					args);

				// 7. Prepare for next batch.
				int currentlyInserted = scriptResult.Response;
				currentCount += currentlyInserted;
			}

			return currentCount;
		}

		private static string CreateBulkInsertScriptArguments(List<DocumentDBHash> docs, int currentIndex, int maxCount, int maxScriptSize)
		{
			var jsonDocumentArray = new StringBuilder(1000);

			if (currentIndex >= maxCount) return string.Empty;

			string serialized = JsonConvert.SerializeObject(docs[currentIndex]);
			jsonDocumentArray.Append("[").Append(serialized);

			int scriptCapacityRemaining = maxScriptSize;

			int i = 1;
			while (jsonDocumentArray.Length < scriptCapacityRemaining && (currentIndex + i) < maxCount)
			{
				jsonDocumentArray.Append(", ").Append(JsonConvert.SerializeObject(docs[currentIndex + i]));
				i++;
			}

			jsonDocumentArray.Append("]");
			return jsonDocumentArray.ToString();
		}

		public async Task<IEnumerable<ThinHashes>> AutoComplete(string text)
		{
			text = text.Trim();
			IEnumerable<DocumentDBHash> docs = await base.GetItemsAsync(d => d.HashMD5.StartsWith(text) || d.HashSHA256.StartsWith(text), 20);
			return docs.DefaultIfEmpty(new ThinHashes { Key = _NOTHING_FOUND_TEXT });
		}

		/// <summary>
		/// paging (offset-limit approach) implementation
		/// </summary>
		/// <param name="sortColumn"></param>
		/// <param name="sortOrderDirection"></param>
		/// <param name="searchText"></param>
		/// <param name="offset"></param>
		/// <param name="limit"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task<(IEnumerable<ThinHashes> Itemz, int Count)> SearchAsync(string sortColumn, string sortOrderDirection, string searchText,
			int offset, int limit, CancellationToken token)
		{
			//IDocumentQuery<ThinHashes> query;
			limit = limit > 0 ? limit : -1;

			IEnumerable<string> columnNames = new string[] { "Key", "HashMD5", "HashSHA256" };
			var query = _client.CreateDocumentQuery<DocumentDBHash>(
				UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
				new FeedOptions { MaxItemCount = limit })
				//.Where(FilteraDoo)
				.AsQueryable()
				//.AsDocumentQuery()
				;
			if (searchText?.Length > 0)
			{
				query = query
					.Where(FilteraDoo)
					.AsQueryable();
			}
			if (sortColumn?.Length > 0)
			{
				if (sortOrderDirection.ToUpperInvariant() == "ASC")
					query = query.OrderBy(OrderaDoo).AsQueryable();
				else
					query = query.OrderByDescending(OrderaDoo).AsQueryable();
			}

			int count = query.Count();
			if (limit <= 0)
				limit = count;
			query = query.Skip(offset).Take(limit);

			var asDocument = query.AsDocumentQuery();
			var results = new List<ThinHashes>(limit);
			while (asDocument.HasMoreResults)
			{
				results.AddRange(await asDocument.ExecuteNextAsync<ThinHashes>());
			}

			return (results, count);

			//inner method
			object OrderaDoo(DocumentDBHash arg)
			{
				object value = typeof(DocumentDBHash).GetProperty(sortColumn,
					BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)
					.GetValue(arg);
				return value;
			}

			//inner method
			bool FilteraDoo(DocumentDBHash arg)
			{
				foreach (var col in columnNames)
				{
					object value = typeof(DocumentDBHash).GetProperty(col,
						BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)
						.GetValue(arg);

					//if (value?.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
					if (value != null && value.ToString().StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
						return true;
				}

				return false;
			}
		}

		public Task<HashesInfo> CalculateHashesInfo(ILoggerFactory _loggerFactory, ILogger _logger, IConfiguration conf,
			DbContextOptions<BloggingContext> dbContextOptions)
		{
			//throw new NotImplementedException();
			//TODO: propertly implement
			return Task.FromResult(
				new HashesInfo
				{
					ID = 0,
					Alphabet = "fake",
					Count = 1234567,
					IsCalculating = false,
					KeyLength = 5
				});
		}

		/// <summary>
		/// Searching for exact matching item
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public async Task<ThinHashes> SearchAsync(HashInput input)
		{
			int itemsCount = 1;
			IDocumentQuery<DocumentDBHash> query;

			if (input.Kind == KindEnum.MD5)
			{
				query = _client.CreateDocumentQuery<DocumentDBHash>(
					UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
					new FeedOptions { MaxItemCount = itemsCount/*, EnableCrossPartitionQuery = true*/ })
					.Where(d => d.HashMD5 == input.Search)
					.AsDocumentQuery();
			}
			else
			{
				query = _client.CreateDocumentQuery<DocumentDBHash>(
					UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
					new FeedOptions { MaxItemCount = itemsCount/*, EnableCrossPartitionQuery = true*/ })
					.Where(d => d.HashSHA256 == input.Search)
					.AsDocumentQuery();
			}

			List<DocumentDBHash> results = new List<DocumentDBHash>(20);
			while (query.HasMoreResults)
			{
				results.AddRange(await query.ExecuteNextAsync<DocumentDBHash>());
			}

			return results.DefaultIfEmpty(new ThinHashes { Key = _NOTHING_FOUND_TEXT })
				.FirstOrDefault();
		}

		#region Dummy implementation fillings

		public void SetReadOnly(bool value)
		{
			//dummy
		}

		public ThinHashes Add(ThinHashes entity)
		{
			throw new NotImplementedException();
		}

		public Task<ThinHashes> AddAsync(ThinHashes entity)
		{
			throw new NotImplementedException();
		}

		public Task AddRangeAsync(IEnumerable<ThinHashes> entities)
		{
			throw new NotImplementedException();
		}

		public void Delete(ThinHashes entity)
		{
			throw new NotImplementedException();
		}

		public void DeleteRange(IEnumerable<ThinHashes> entities)
		{
			throw new NotImplementedException();
		}

		public void DeleteAll()
		{
			throw new NotImplementedException();
		}

		public void Edit(ThinHashes entity)
		{
			throw new NotImplementedException();
		}

		public IQueryable<ThinHashes> FindBy(Expression<Func<ThinHashes, bool>> predicate)
		{
			throw new NotImplementedException();
		}

		public Task<List<ThinHashes>> FindByAsync(Expression<Func<ThinHashes, bool>> predicate)
		{
			throw new NotImplementedException();
		}

		public Task<ThinHashes> GetSingleAsync(params object[] keyValues)
		{
			throw new NotImplementedException();
		}

		public IQueryable<ThinHashes> GetAll()
		{
			throw new NotImplementedException();
		}

		public Task<List<ThinHashes>> GetAllAsync()
		{
			throw new NotImplementedException();
		}

		public int Save()
		{
			throw new NotImplementedException();
		}

		public Task<int> SaveAsync()
		{
			throw new NotImplementedException();
		}

		#endregion Dummy implementation fillings

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
					base.Cleanup();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~ThinHashesDocumentDBRepository() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
