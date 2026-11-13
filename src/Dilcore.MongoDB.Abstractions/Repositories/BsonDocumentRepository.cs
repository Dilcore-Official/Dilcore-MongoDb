using Dilcore.MongoDB.Abstractions.Keys;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Repositories;

/// <summary>
/// Base helper for repositories that work with untyped <see cref="BsonDocument"/> collections and resolved names.
/// </summary>
public abstract class BsonDocumentRepository
{
    private readonly MongoDatabaseKey _databaseKey;
    private readonly IMongoDbCollectionFactory _collectionFactory;
    private readonly string? _staticPrefix;

    /// <summary>
    /// Binds the repository to a database key and collection factory.
    /// </summary>
    /// <param name="databaseKey">Registered database binding key.</param>
    /// <param name="collectionFactory">Factory that applies namespace prefixes and returns collections.</param>
    /// <param name="staticPrefix">Optional fixed prefix when the binding does not use a scoped resolver.</param>
    protected BsonDocumentRepository(
        MongoDatabaseKey databaseKey,
        IMongoDbCollectionFactory collectionFactory,
        string? staticPrefix = null)
    {
        _databaseKey = databaseKey;
        _collectionFactory = collectionFactory;
        _staticPrefix = staticPrefix;
    }

    /// <summary>
    /// Resolves and returns the <see cref="BsonDocument"/> collection for <paramref name="collectionName"/>.
    /// </summary>
    /// <param name="collectionName">Logical collection name from registration.</param>
    /// <param name="cancellationToken">Token used to cancel resolution.</param>
    /// <returns>A successful result with the collection; otherwise a failed result.</returns>
    protected Task<Result<IMongoCollection<BsonDocument>>> GetCollectionAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        return _collectionFactory.GetCollectionAsync(
            _databaseKey,
            collectionName,
            _staticPrefix,
            cancellationToken);
    }

    /// <summary>
    /// Returns the physical collection name after prefix resolution.
    /// </summary>
    /// <param name="collectionName">Logical collection name from registration.</param>
    /// <param name="cancellationToken">Token used to cancel resolution.</param>
    /// <returns>A successful result with the resolved name; otherwise a failed result.</returns>
    protected Task<Result<string>> GetCollectionNameAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        return _collectionFactory.ResolveCollectionNameAsync(
            _databaseKey,
            collectionName,
            _staticPrefix,
            cancellationToken);
    }

    /// <summary>
    /// Resolves the collection and runs <paramref name="execution"/>; maps <see cref="MongoException"/> to a failed result.
    /// </summary>
    /// <param name="collectionName">Logical collection name from registration.</param>
    /// <param name="execution">Work to perform on the resolved collection.</param>
    /// <param name="cancellationToken">Token passed to collection resolution (not automatically forwarded to <paramref name="execution"/>).</param>
    /// <returns>The result from <paramref name="execution"/>, or a failure from resolution or an unhandled driver error.</returns>
    protected async Task<Result> ExecuteAsync(
        string collectionName,
        Func<IMongoCollection<BsonDocument>, Task<Result>> execution,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = await GetCollectionAsync(collectionName, cancellationToken);

            if (collection.IsFailed)
            {
                return collection.ToResult();
            }

            return await execution(collection.Value);
        }
        catch (MongoException e)
        {
            return Result.Fail(new Error(e.Message));
        }
    }

    /// <summary>
    /// Resolves the collection and runs <paramref name="execution"/>.
    /// </summary>
    /// <typeparam name="T">Result payload type.</typeparam>
    /// <param name="collectionName">Logical collection name from registration.</param>
    /// <param name="execution">Work to perform on the resolved collection.</param>
    /// <param name="cancellationToken">Token passed to collection resolution (not automatically forwarded to <paramref name="execution"/>).</param>
    /// <returns>The result from <paramref name="execution"/>, or a failure from collection resolution.</returns>
    protected async Task<Result<T>> ExecuteAsync<T>(
        string collectionName,
        Func<IMongoCollection<BsonDocument>, Task<Result<T>>> execution,
        CancellationToken cancellationToken = default)
    {
        var collection = await GetCollectionAsync(collectionName, cancellationToken);

        if (collection.IsFailed)
        {
            return collection.ToResult();
        }

        return await execution(collection.Value);
    }
}