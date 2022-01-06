#if INCLUDE_MONGODB

using MongoDB.Driver;

namespace DotnetPlayground.Repositories.Mongo
{
	public class MongoService
	{
		private static MongoClient _client;

		public MongoClient Client => _client;

		public MongoService(IMongoDBSettings settings)
		{
			//{
			// "MongoConnectionString": "mongodb://user:passss@127.0.0.1:27017/?authSource=authdb&tls=true&tlsInsecure=true&directConnection=true&serverSelectionTimeoutMS=2000",
			//}
			_client = new MongoClient(settings.MongoConnectionString);
		}
	}
}

#endif