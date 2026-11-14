using Dilcore.MongoDB.Abstractions.Internal;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Policies;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Repositories;

/// <summary>
/// Shared infrastructure for typed MongoDB repositories: collection resolution, soft-delete filters, and guarded execution.
/// </summary>
/// <typeparam name="TDocument">Document type stored in the collection.</typeparam>
public abstract class BaseMongoDbRepository<TDocument> where TDocument : class, IDocumentEntity
{
    private static readonly bool IsSoftDeletable =
        typeof(ISoftDeletable).IsAssignableFrom(typeof(TDocument));

    private static readonly FilterDefinition<TDocument> NotDeletedFilter =
        SoftDeleteFilterCache.GetNotDeletedFilter<TDocument>();

    private readonly Func<CancellationToken, Task<Result<IMongoCollection<TDocument>>>> _collectionProvider;
    private readonly GetCollectionOptions<TDocument> _options;

    /// <summary>
    /// Initializes the repository with collection options and a provider that resolves the target collection.
    /// </summary>
    /// <param name="optionsAction">Configures <see cref="GetCollectionOptions{TDocument}"/> for this repository.</param>
    /// <param name="collectionProvider">Returns the collection for the current scope; failures propagate to callers.</param>
    protected BaseMongoDbRepository(
        Action<GetCollectionOptions<TDocument>> optionsAction,
        Func<CancellationToken, Task<Result<IMongoCollection<TDocument>>>> collectionProvider)
    {
        _collectionProvider = collectionProvider;

        var options = new GetCollectionOptions<TDocument>();
        optionsAction(options);
        _options = options;
    }

    /// <summary>
    /// Optional transactional session bound to repository operations in the current scope.
    /// </summary>
    protected IClientSessionHandle? Session { get; init; }

    internal IMongoOperationBudget? Budget { get; init; }

    /// <summary>
    /// Resolves the collection, optionally reserves transaction budget, runs <paramref name="func"/>, and maps driver exceptions to typed errors.
    /// </summary>
    /// <param name="func">Operation to run against the resolved collection.</param>
    /// <param name="estimatedBytes">Estimated BSON bytes to reserve against an active transaction budget, when present.</param>
    /// <param name="cancellationToken">Token passed through to collection resolution and <paramref name="func"/>.</param>
    /// <returns>
    /// The result from <paramref name="func"/>; collection resolution failures and mapped MongoDB errors are returned as failed <see cref="Result{TDocument}"/>.
    /// </returns>
    protected Task<Result<TDocument>> ExecuteAsync(
        Func<IMongoCollection<TDocument>, CancellationToken, Task<Result<TDocument>>> func,
        int? estimatedBytes = 256,
        CancellationToken cancellationToken = default)
        => ExecuteMappedAsync(func, result => Result.Fail<TDocument>(result.Errors), estimatedBytes, cancellationToken);

    /// <summary>
    /// Resolves the collection, optionally reserves transaction budget, runs <paramref name="func"/>, and maps driver exceptions to typed errors.
    /// </summary>
    /// <param name="func">Operation to run against the resolved collection.</param>
    /// <param name="estimatedBytes">Estimated BSON bytes to reserve against an active transaction budget, when present.</param>
    /// <param name="cancellationToken">Token passed through to collection resolution and <paramref name="func"/>.</param>
    /// <returns>
    /// The result from <paramref name="func"/>; collection resolution failures and mapped MongoDB errors are returned as failed results.
    /// </returns>
    protected Task<Result<IReadOnlyList<TDocument>>> ExecuteAsync(
        Func<IMongoCollection<TDocument>, CancellationToken, Task<Result<IReadOnlyList<TDocument>>>> func,
        int? estimatedBytes = 256,
        CancellationToken cancellationToken = default)
        => ExecuteMappedAsync(func, result => Result.Fail<IReadOnlyList<TDocument>>(result.Errors), estimatedBytes, cancellationToken);

    /// <summary>
    /// Resolves the collection, optionally reserves transaction budget, runs <paramref name="func"/>, and maps driver exceptions to typed errors.
    /// </summary>
    /// <param name="func">Operation to run against the resolved collection.</param>
    /// <param name="estimatedBytes">Estimated BSON bytes to reserve against an active transaction budget, when present.</param>
    /// <param name="cancellationToken">Token passed through to collection resolution and <paramref name="func"/>.</param>
    /// <returns>
    /// The result from <paramref name="func"/>; failed results may include a typed <see cref="Results.MongoOperationError"/>.
    /// </returns>
    protected Task<Result> ExecuteAsync(
        Func<IMongoCollection<TDocument>, CancellationToken, Task<Result>> func,
        int? estimatedBytes = 256,
        CancellationToken cancellationToken = default)
        => ExecuteMappedAsync(func, result => Result.Fail(result.Errors), estimatedBytes, cancellationToken);

    /// <summary>
    /// Resolves the collection, optionally reserves transaction budget, runs <paramref name="func"/>, and maps driver exceptions to typed errors.
    /// </summary>
    /// <typeparam name="TResult">Payload type returned by <paramref name="func"/>.</typeparam>
    /// <param name="func">Operation to run against the resolved collection.</param>
    /// <param name="estimatedBytes">Estimated BSON bytes to reserve against an active transaction budget, when present.</param>
    /// <param name="cancellationToken">Token passed through to collection resolution and <paramref name="func"/>.</param>
    /// <returns>
    /// The result from <paramref name="func"/>; collection resolution failures and mapped MongoDB errors are returned as failed <see cref="Result{TResult}"/>.
    /// </returns>
    protected Task<Result<TResult>> ExecuteAsync<TResult>(
        Func<IMongoCollection<TDocument>, CancellationToken, Task<Result<TResult>>> func,
        int? estimatedBytes = 256,
        CancellationToken cancellationToken = default)
        => ExecuteMappedAsync(func, result => Result.Fail<TResult>(result.Errors), estimatedBytes, cancellationToken);

    private async Task<Result<TResult>> ExecuteMappedAsync<TResult>(
        Func<IMongoCollection<TDocument>, CancellationToken, Task<Result<TResult>>> func,
        Func<Result<IMongoCollection<TDocument>>, Result<TResult>> errorHandler,
        int? estimatedBytes = null,
        CancellationToken cancellationToken = default)
    {
        var collectionResult = await GetCollectionAsync(cancellationToken);
        if (collectionResult.IsFailed)
        {
            return errorHandler(collectionResult);
        }

        if (Budget is not null && estimatedBytes is int bytes)
        {
            var reservation = Budget.Reserve(bytes);
            if (reservation.IsFailed)
            {
                return Result.Fail<TResult>(reservation.Errors);
            }
        }

        try
        {
            return await func(collectionResult.Value, cancellationToken);
        }
        catch (Exception exception) when (exception is MongoException or FormatException)
        {
            if (Session is not null && IsDriverRetryable(exception))
            {
                throw;
            }

            return MongoExceptionMapper.Fail<TResult>(exception);
        }
    }

    private async Task<Result> ExecuteMappedAsync(
        Func<IMongoCollection<TDocument>, CancellationToken, Task<Result>> func,
        Func<Result<IMongoCollection<TDocument>>, Result> errorHandler,
        int? estimatedBytes = null,
        CancellationToken cancellationToken = default)
    {
        var collectionResult = await GetCollectionAsync(cancellationToken);
        if (collectionResult.IsFailed)
        {
            return errorHandler(collectionResult);
        }

        if (Budget is not null && estimatedBytes is int bytes)
        {
            var reservation = Budget.Reserve(bytes);
            if (reservation.IsFailed)
            {
                return Result.Fail(reservation.Errors);
            }
        }

        try
        {
            return await func(collectionResult.Value, cancellationToken);
        }
        catch (Exception exception) when (exception is MongoException or FormatException)
        {
            if (Session is not null && IsDriverRetryable(exception))
            {
                throw;
            }

            return MongoExceptionMapper.Fail(exception);
        }
    }

    private static bool IsDriverRetryable(Exception exception)
        => exception is MongoException mongo
            && (mongo.HasErrorLabel("TransientTransactionError")
                || mongo.HasErrorLabel("UnknownTransactionCommitResult"));

    /// <summary>
    /// Returns the collection options configured for this repository instance.
    /// </summary>
    /// <returns>Immutable options snapshot used for soft-delete and related policies.</returns>
    protected GetCollectionOptions<TDocument> GetOptions() => _options;

    /// <summary>
    /// Combines <paramref name="filter"/> with a not-deleted constraint when soft delete is enabled for <typeparamref name="TDerived"/>.
    /// </summary>
    /// <typeparam name="TDerived">Derived document type used in the filter.</typeparam>
    /// <param name="filter">Caller-supplied filter.</param>
    /// <returns>The original filter, or the filter ANDed with a not-deleted predicate.</returns>
    protected FilterDefinition<TDerived> ApplyNotDeleteFilter<TDerived>(FilterDefinition<TDerived> filter)
        where TDerived : class, TDocument
    {
        if (_options.SoftDeleteDisabled || !IsSoftDeletable)
        {
            return filter;
        }

        return filter & SoftDeleteFilterCache.GetNotDeletedFilter<TDerived>();
    }

    /// <summary>
    /// Combines <paramref name="filter"/> with a not-deleted constraint when soft delete is enabled.
    /// </summary>
    /// <param name="filter">Caller-supplied filter.</param>
    /// <returns>The original filter, or the filter ANDed with a not-deleted predicate.</returns>
    protected FilterDefinition<TDocument> ApplyNotDeleteFilter(FilterDefinition<TDocument> filter)
    {
        if (_options.SoftDeleteDisabled || !IsSoftDeletable)
        {
            return filter;
        }

        return filter & NotDeletedFilter;
    }

    /// <summary>
    /// Returns a filter that excludes soft-deleted documents when the feature is enabled; otherwise an empty filter.
    /// </summary>
    /// <returns>Not-deleted filter or an empty filter when soft delete is disabled.</returns>
    protected FilterDefinition<TDocument> ApplyNotDeleteFilter()
    {
        if (_options.SoftDeleteDisabled || !IsSoftDeletable)
        {
            return Builders<TDocument>.Filter.Empty;
        }

        return NotDeletedFilter;
    }

    /// <summary>
    /// Resolves the MongoDB collection for the current request scope.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel namespace or collection resolution.</param>
    /// <returns>
    /// A successful result with the collection; otherwise a failed result (for example missing namespace prefix).
    /// </returns>
    protected Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync(CancellationToken cancellationToken = default)
    {
        return _collectionProvider(cancellationToken);
    }
}