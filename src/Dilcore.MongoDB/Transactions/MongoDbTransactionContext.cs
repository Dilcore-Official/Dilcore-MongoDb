using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Transactions;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Transactions;

internal sealed class MongoDbTransactionContext : IMongoDbTransactionContext
{
    public MongoDbTransactionContext(
        MongoClusterKey clusterKey,
        IClientSessionHandle session,
        IRepositoryResolver repositories)
    {
        ClusterKey = clusterKey;
        Session = session;
        Repositories = repositories;
    }

    public MongoClusterKey ClusterKey { get; }

    public IClientSessionHandle Session { get; }

    public IRepositoryResolver Repositories { get; }
}