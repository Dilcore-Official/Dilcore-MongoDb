using Dilcore.MongoDB.Abstractions.Ownership;
using MongoDB.Driver;

namespace Dilcore.MongoDB.DependencyInjection;

public interface IMongoClusterBuilder
{
    IMongoClusterBuilder UseConnectionString(string connectionString);

    IMongoClusterBuilder UseMaxConnectionPoolSize(int maxConnectionPoolSize);

    IMongoClusterBuilder UseExistingClient(IMongoClient client, MongoClientOwnership ownership);
}
