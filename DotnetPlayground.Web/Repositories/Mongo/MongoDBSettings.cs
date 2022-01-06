#if INCLUDE_MONGODB

namespace DotnetPlayground.Repositories.Mongo
{
	public interface IMongoDBSettings
	{
		string DatabaseName { get; set; }
		string CollectionName { get; set; }
		string MongoConnectionString { get; set; }
	}

	public class MongoDBSettings : IMongoDBSettings
	{
		public string DatabaseName { get; set; }
		public string CollectionName { get; set; }
		public string MongoConnectionString { get; set; }
	}
}

#endif