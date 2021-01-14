#if INCLUDE_COSMOSDB

using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DotnetPlayground.Repositories
{
	abstract class DocumentDBRepository<T> where T : class
	{
		protected readonly string _endpoint, _key, _databaseId, _collectionId;
		protected DocumentClient _client;

		public DocumentDBRepository(string endpoint, string key, string databaseId, string collectionId)
		{
			_endpoint = endpoint;
			_key = key;
			_databaseId = databaseId;
			_collectionId = collectionId;
		}

		public async Task<T> GetByIDAsync(string id)
		{
			try
			{
				Document document = await _client.ReadDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id));
				return (T)(dynamic)document;
			}
			catch (DocumentClientException e)
			{
				if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					return null;
				}
				else
				{
					throw;
				}
			}
		}

		public async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate, int itemsCount = -1)
		{
			IDocumentQuery<T> query;
			List<T> results;
			if (itemsCount >= 0)
			{
				query = _client.CreateDocumentQuery<T>(
					UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
					new FeedOptions { MaxItemCount = itemsCount })
					.Where(predicate)
					.Take(itemsCount)
					.AsDocumentQuery();
				results = new List<T>(itemsCount);
			}
			else
			{
				query = _client.CreateDocumentQuery<T>(
					UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
					new FeedOptions { MaxItemCount = itemsCount })
					.Where(predicate)
					.AsDocumentQuery();
				results = new List<T>();
			}

			while (query.HasMoreResults)
			{
				results.AddRange(await query.ExecuteNextAsync<T>());
			}

			return results;
		}

		public async Task<Document> CreateItemAsync(T item)
		{
			return await _client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId), item,
				disableAutomaticIdGeneration: true);
		}

		public async Task<Document> UpdateItemAsync(string id, T item)
		{
			return await _client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id), item);
		}

		public async Task DeleteItemAsync(string id)
		{
			await _client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id));
		}
		
		/// <summary>
		/// Initialize Document DB client access object
		/// </summary>
		protected abstract void Initialize();

		/// <summary>
		/// Clean up client object which is IDisposable
		/// </summary>
		protected void Cleanup()
		{
			if (_client != null)
			{
				_client.Dispose();
				_client = null;
			}
		}

		protected async Task CreateDatabaseIfNotExistsAsync()
		{
			try
			{
				await _client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(_databaseId));
			}
			catch (DocumentClientException e)
			{
				if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					await _client.CreateDatabaseAsync(new Database { Id = _databaseId });
				}
				else
				{
					throw;
				}
			}
		}

		protected async Task CreateCollectionIfNotExistsAsync(DocumentCollection myNewCollection)
		{
			try
			{
				await _client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId));
			}
			catch (DocumentClientException e)
			{
				if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					await _client.CreateDocumentCollectionAsync(
						UriFactory.CreateDatabaseUri(_databaseId),
						myNewCollection,
						new RequestOptions { OfferThroughput = 2500 });
				}
				else
				{
					throw;
				}
			}
		}

		protected async Task CreateSprocIfNotExists(string scriptFileName, string scriptId, string scriptName)
		{
			var client = new DocumentClient(new Uri(_endpoint), _key);
			Uri collectionLink = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId);

			var sproc = new StoredProcedure
			{
				Id = scriptId,
				Body = File.ReadAllText(scriptFileName)
			};

			bool needToCreate = false;
			Uri sprocUri = UriFactory.CreateStoredProcedureUri(_databaseId, _collectionId, scriptName);

			try
			{
				await client.ReadStoredProcedureAsync(sprocUri);
			}
			catch (DocumentClientException de)
			{
				if (de.StatusCode != System.Net.HttpStatusCode.NotFound)
				{
					throw;
				}
				else
				{
					needToCreate = true;
				}
			}

			if (needToCreate)
			{
				await client.CreateStoredProcedureAsync(collectionLink, sproc);
			}
		}
	}
}

#endif