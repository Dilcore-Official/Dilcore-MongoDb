using Dilcore.MongoDB.Abstractions.Keys;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Repositories;

public abstract class BsonDocumentRepository
{
    private readonly MongoDatabaseKey _databaseKey;
    private readonly IMongoDbCollectionFactory _collectionFactory;
    private readonly string? _staticPrefix;

    protected BsonDocumentRepository(
        MongoDatabaseKey databaseKey,
        IMongoDbCollectionFactory collectionFactory,
        string? staticPrefix = null)
    {
        _databaseKey = databaseKey;
        _collectionFactory = collectionFactory;
        _staticPrefix = staticPrefix;
    }

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
