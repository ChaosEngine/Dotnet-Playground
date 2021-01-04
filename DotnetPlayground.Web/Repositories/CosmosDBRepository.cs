#if INCLUDE_COSMOSDB

using DotnetPlayground.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetPlayground.Repositories
{
	sealed class DocumentDBHash : ThinHashes
	{
		/// <summary>
		/// Document DB enforces Id node to be (auto)generated
		/// </summary>
		[JsonPropertyName("id")]
		public string Id { get; set; }
	}

	internal interface IThinHashesDocumentDBRepository : IHashesRepositoryPure
	{
		Task<IEnumerable<DocumentDBHash>> GetItemsSortedDescByKeyAsync(int itemsCount = -1);
		Task<DocumentDBHash> GetByIDAsync(string id);
		Task<IEnumerable<DocumentDBHash>> GetItemsAsync(Expression<Func<DocumentDBHash, bool>> predicate, int itemsCount = -1);
	}

	internal class ThinHashesDocumentDBRepository : DocumentDBRepository<DocumentDBHash>, IThinHashesDocumentDBRepository, IDisposable
	{
		private const string _NOTHING_FOUND_TEXT = "nothing found";
		/// <summary>
		/// TODO: The transaction identifier - kill it!
		/// </summary>
		private Guid _transactionId;
		/// <summary>
		/// Used value or this specific worker node/process or load balancing server
		/// </summary>
		private static HashesInfo _hashesInfoStatic;
		/// <summary>
		/// locally cached value for request, refreshed upon every request.
		/// </summary>
		private HashesInfo _hi;

		public Task<HashesInfo> CurrentHashesInfo
		{
			get { return GetHashesInfoFromDB(); }
		}

		/// <summary>
		/// Gets the hashes information from database.
		/// </summary>
		/// <returns></returns>
		private async Task<HashesInfo> GetHashesInfoFromDB()
		{
			if (_hashesInfoStatic == null)
			{
				if (_hi == null)            //local value is empty, fill it from DB once
					_hi = null;//await CalculateHashesInfo(null, null, null, null);

				if (_hi == null || _hi.IsCalculating)
					return _hi;             //still calculating, return just this local value
				else
					_hashesInfoStatic = _hi;//calculation ended, save to global static value
			}
			return await Task.FromResult(_hashesInfoStatic);
		}

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

				var args = new dynamic[] { JsonSerializer.Deserialize<dynamic>(argsJson) };

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

			string serialized = JsonSerializer.Serialize(docs[currentIndex]);
			jsonDocumentArray.Append("[").Append(serialized);

			int scriptCapacityRemaining = maxScriptSize;

			int i = 1;
			while (jsonDocumentArray.Length < scriptCapacityRemaining && (currentIndex + i) < maxCount)
			{
				jsonDocumentArray.Append(", ").Append(JsonSerializer.Serialize(docs[currentIndex + i]));
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
		public async Task<(IEnumerable<string[]> Itemz, int Count)> PagedSearchAsync(string sortColumn, string sortOrderDirection, string searchText,
			int offset, int limit, CancellationToken token)
		{
			limit = limit > 0 ? limit : -1;
			var collectionLink = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId);

			IEnumerable<string> columnNames = new string[] { "Key", "HashMD5", "HashSHA256" };
			StringBuilder sb = new StringBuilder(750);
			string separator = string.Empty;
			if (searchText?.Length > 0)
			{
				sb.Append("WHERE (");
				string comma = string.Empty;
				foreach (string fieldName in columnNames)
				{
					sb.AppendFormat("{1}STARTSWITH(c.{0}, \"{2}\")", fieldName, comma, searchText);
					comma = " OR\r\n";
				}
				sb.Append(")");
				separator = "\r\nAND";
			}
			else
				separator = "\r\nWHERE ";

			string query_str = "SELECT VALUE count(1) FROM c " + sb.ToString()/* + "\r\nORDER BY c.Key DESC"*/;
			int count = (await _client.CreateDocumentQuery<int>(collectionLink, query_str)
				.AsDocumentQuery().ExecuteNextAsync<int>())
				.FirstOrDefault();

			//TODO: not working!
			//query = query.Where(c => c.Id.CompareTo(offset.ToString()) > 0).Take(limit);
			if (offset > 0)
				sb.AppendFormat("{1} udf.ConvertToNumber(c.id) > {0}", offset, separator);

			if (sortColumn?.Length > 0)
			{
				sb.AppendFormat("\r\nORDER BY c.{0} {1}", sortColumn, sortOrderDirection);
			}

			query_str = string.Format("SELECT TOP {0} c.Key, c.HashMD5, c.HashSHA256 FROM c " + sb.ToString(), limit);
			var query = _client.CreateDocumentQuery<DocumentDBHash>(collectionLink,
				query_str,
				new FeedOptions { MaxItemCount = limit, MaxDegreeOfParallelism = -1 })
				.AsQueryable();

			var asDocument = query.AsDocumentQuery();
			var results = new List<ThinHashes>(limit);
			while (asDocument.HasMoreResults)
			{
				results.AddRange(await asDocument.ExecuteNextAsync<ThinHashes>());
			}

			return (results.Select(x => new string[] { x.Key, x.HashMD5, x.HashSHA256 }), count);

			#region Old code
			/*//inner method
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
			}*/
			#endregion Old code
		}

		public async Task<HashesInfo> CalculateHashesInfo(ILogger logger, Microsoft.EntityFrameworkCore.DbContextOptions<BloggingContext> dbContextOptions,
			CancellationToken token = default)
		{
			using (var client = new DocumentClient(new Uri(_endpoint), _key))
			{
				HashesInfo hi = null;
				try
				{
					if (GetHashesInfoFromDB().Result != null)
					{
						if (logger != null)
							logger.LogInformation(0, $"###Leaving calculation of initial Hash parameters; already present");
						return GetHashesInfoFromDB().Result;
					}
					if (logger != null)
						logger.LogInformation(0, $"###Starting calculation of initial Hash parameters");

					hi = new HashesInfo { ID = 0, IsCalculating = true };

					_hashesInfoStatic = hi;//temporary save to static to indicate calculation and block new calcultion threads


					var collection_link = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId);
					//var alphabet = client.CreateDocumentQuery<DocumentDBHash>(collection_link)
					//	.Select(f => f.Key.First())
					//	.Distinct()
					//	.OrderBy(o => o);
					int.TryParse(client.CreateDocumentQuery<DocumentDBHash>(collection_link)
						.OrderByDescending(x => x.Key).Take(1).ToArray().FirstOrDefault().Id, out int count);
					var key_length = client.CreateDocumentQuery<int>(collection_link,
						"SELECT TOP 1 VALUE LENGTH(c.Key) FROM c").FirstOrDefaultAsync();

					hi.Count = count;
					hi.KeyLength = await key_length;
					hi.Alphabet = "fakefakefake";//string.Concat(alphabet);
					hi.IsCalculating = false;

					if (logger != null)
						logger.LogInformation(0, $"###Calculation of initial Hash parameters ended");
				}
				catch (Exception ex)
				{
					if (logger != null)
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

#endif