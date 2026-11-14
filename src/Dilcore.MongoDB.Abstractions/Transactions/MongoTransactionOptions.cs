using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Transactions;

/// <summary>
/// Client-side limits and driver options for a multi-document transaction.
/// </summary>
public sealed class MongoTransactionOptions
{
    /// <summary>
    /// Maximum repository operations allowed before the budget guard fails the transaction.
    /// </summary>
    public int MaxOperations { get; init; } = 1_000;

    /// <summary>
    /// Maximum estimated BSON bytes writers may reserve across the transaction.
    /// </summary>
    public int MaxEstimatedBytes { get; init; } = 16 * 1024 * 1024;

    /// <summary>
    /// Wall-clock limit for the transaction attempt before it is aborted.
    /// </summary>
    public TimeSpan TimeLimit { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Optional <see cref="TransactionOptions"/> passed to the MongoDB driver session.
    /// </summary>
    public TransactionOptions? DriverOptions { get; init; }
}