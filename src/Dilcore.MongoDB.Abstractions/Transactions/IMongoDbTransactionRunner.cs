using Dilcore.MongoDB.Abstractions.Keys;
using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Transactions;

/// <summary>
/// Starts driver transactions on a cluster and runs callbacks with transactional repositories.
/// </summary>
public interface IMongoDbTransactionRunner
{
    /// <summary>
    /// Runs <paramref name="callback"/> inside a multi-document transaction on <paramref name="clusterKey"/>.
    /// </summary>
    /// <param name="clusterKey">Registered cluster to target.</param>
    /// <param name="callback">Work using <see cref="IMongoDbTransactionContext"/>; a failed result aborts the transaction.</param>
    /// <param name="options">Optional operation, byte, and time budgets.</param>
    /// <param name="cancellationToken">Token used to cancel the transaction.</param>
    /// <returns>
    /// A successful result when the transaction commits; otherwise a failed result (callback failure, unknown cluster, or budget exceeded).
    /// </returns>
    Task<Result> ExecuteAsync(
        MongoClusterKey clusterKey,
        Func<IMongoDbTransactionContext, CancellationToken, Task<Result>> callback,
        MongoTransactionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="callback"/> inside a multi-document transaction and returns its payload on success.
    /// </summary>
    /// <typeparam name="TResult">Type returned by <paramref name="callback"/>.</typeparam>
    /// <param name="clusterKey">Registered cluster to target.</param>
    /// <param name="callback">Work using <see cref="IMongoDbTransactionContext"/>.</param>
    /// <param name="options">Optional operation, byte, and time budgets.</param>
    /// <param name="cancellationToken">Token used to cancel the transaction.</param>
    /// <returns>
    /// A successful result with the callback value when committed; otherwise a failed result that may include <see cref="Results.TransactionBudgetExceededError"/> or callback errors.
    /// </returns>
    Task<Result<TResult>> ExecuteAsync<TResult>(
        MongoClusterKey clusterKey,
        Func<IMongoDbTransactionContext, CancellationToken, Task<Result<TResult>>> callback,
        MongoTransactionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Alias for <see cref="ExecuteAsync(MongoClusterKey, Func{IMongoDbTransactionContext, CancellationToken, Task{Result}}, MongoTransactionOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="clusterKey">Registered cluster to target.</param>
    /// <param name="callback">Transactional work.</param>
    /// <param name="options">Optional budgets.</param>
    /// <param name="cancellationToken">Token used to cancel the transaction.</param>
    /// <returns>The same outcome as <see cref="ExecuteAsync(MongoClusterKey, Func{IMongoDbTransactionContext, CancellationToken, Task{Result}}, MongoTransactionOptions?, CancellationToken)"/>.</returns>
    Task<Result> WithTransactionAsync(
        MongoClusterKey clusterKey,
        Func<IMongoDbTransactionContext, CancellationToken, Task<Result>> callback,
        MongoTransactionOptions? options = null,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(clusterKey, callback, options, cancellationToken);

    /// <summary>
    /// Alias for <see cref="ExecuteAsync{TResult}(MongoClusterKey, Func{IMongoDbTransactionContext, CancellationToken, Task{Result{TResult}}}, MongoTransactionOptions?, CancellationToken)"/>.
    /// </summary>
    /// <typeparam name="TResult">Type returned by <paramref name="callback"/>.</typeparam>
    /// <param name="clusterKey">Registered cluster to target.</param>
    /// <param name="callback">Transactional work.</param>
    /// <param name="options">Optional budgets.</param>
    /// <param name="cancellationToken">Token used to cancel the transaction.</param>
    /// <returns>The same outcome as the generic <see cref="ExecuteAsync{TResult}"/> overload.</returns>
    Task<Result<TResult>> WithTransactionAsync<TResult>(
        MongoClusterKey clusterKey,
        Func<IMongoDbTransactionContext, CancellationToken, Task<Result<TResult>>> callback,
        MongoTransactionOptions? options = null,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(clusterKey, callback, options, cancellationToken);
}