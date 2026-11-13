using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Repositories;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Transactions;

/// <summary>
/// Ambient state exposed to transactional callbacks: cluster, session, and scoped repositories.
/// </summary>
public interface IMongoDbTransactionContext
{
    /// <summary>
    /// Cluster key for the active transaction.
    /// </summary>
    MongoClusterKey ClusterKey { get; }

    /// <summary>
    /// Driver session handle participating in the transaction.
    /// </summary>
    IClientSessionHandle Session { get; }

    /// <summary>
    /// Repository resolver bound to the transactional session and budget.
    /// </summary>
    IRepositoryResolver Repositories { get; }
}