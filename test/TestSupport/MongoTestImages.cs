using Testcontainers.MongoDb;

namespace Dilcore.MongoDB.TestSupport;

/// <summary>
/// Single pin for MongoDB Testcontainers images used by integration tests and benchmarks.
/// </summary>
public static class MongoTestImages
{
    public const string Mongo = "mongo:7.0";

    public static MongoDbContainer CreateStandalone()
        => new MongoDbBuilder(Mongo).Build();

    public static MongoDbContainer CreateReplicaSet(string replicaSetName = "rs0")
        => new MongoDbBuilder(Mongo).WithReplicaSet(replicaSetName).Build();
}
