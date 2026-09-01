using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Repositories;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Transactions;

public sealed class MongoTransactionOptions
{
    public int MaxOperations { get; init; } = 1_000;

    public int MaxEstimatedBytes { get; init; } = 16 * 1024 * 1024;

    public TimeSpan TimeLimit { get; init; } = TimeSpan.FromSeconds(60);

    public TransactionOptions? DriverOptions { get; init; }
}

public interface IMongoDbTransactionContext
{
    MongoClusterKey ClusterKey { get; }

    IClientSessionHandle Session { get; }

    IRepositoryResolver Repositories { get; }
}

public interface IMongoDbTransactionRunner
{
    Task<Result> ExecuteAsync(
        MongoClusterKey clusterKey,
        Func<IMongoDbTransactionContext, CancellationToken, Task<Result>> callback,
        MongoTransactionOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<Result<TResult>> ExecuteAsync<TResult>(
        MongoClusterKey clusterKey,
        Func<IMongoDbTransactionContext, CancellationToken, Task<Result<TResult>>> callback,
        MongoTransactionOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<Result> WithTransactionAsync(
        MongoClusterKey clusterKey,
        Func<IMongoDbTransactionContext, CancellationToken, Task<Result>> callback,
        MongoTransactionOptions? options = null,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(clusterKey, callback, options, cancellationToken);

    Task<Result<TResult>> WithTransactionAsync<TResult>(
        MongoClusterKey clusterKey,
        Func<IMongoDbTransactionContext, CancellationToken, Task<Result<TResult>>> callback,
        MongoTransactionOptions? options = null,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(clusterKey, callback, options, cancellationToken);
}
