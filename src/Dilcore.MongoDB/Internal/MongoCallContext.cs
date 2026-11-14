using MongoDB.Driver;

namespace Dilcore.MongoDB.Internal;

internal sealed class MongoCallContext
{
    public static MongoCallContext None { get; } = new();

    public IClientSessionHandle? Session { get; init; }

    public ITransactionBudgetGuard? Budget { get; init; }

    public MongoClusterKeyBound? Cluster { get; init; }
}