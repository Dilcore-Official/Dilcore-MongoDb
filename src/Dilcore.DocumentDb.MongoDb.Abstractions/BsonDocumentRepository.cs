using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.Abstractions;

public abstract class BsonDocumentRepository : IBsonDocumentRepository
{
    private readonly string _databaseName;
    private readonly IBsonDocumentCollectionFactory _bsonDocumentCollectionFactory;
    
    protected BsonDocumentRepository(string databaseName, IBsonDocumentCollectionFactory bsonDocumentCollectionFactory)
    {
        _databaseName = databaseName;
        _bsonDocumentCollectionFactory = bsonDocumentCollectionFactory;
    }
    
    protected async Task<Result<IMongoCollection<BsonDocument>>> GetCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        return await _bsonDocumentCollectionFactory.GetCollectionAsync(_databaseName, collectionName, cancellationToken);
    }
    
    protected async Task<Result<string>> GetCollectionNameAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        return await _bsonDocumentCollectionFactory.GetCollectionNameAsync(_databaseName, collectionName, cancellationToken);
    }
    
    protected async Task<Result> ExecuteAsync(string collectionName, Func<IMongoCollection<BsonDocument>, Task<Result>> execution, CancellationToken cancellationToken = default)
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
        catch (Exception e)
        {
            return Result.Fail(new Error(e.Message));
        }
    }
    
    protected async Task<Result<T>> ExecuteAsync<T>(string collectionName, Func<IMongoCollection<BsonDocument>, Task<Result<T>>> execution, CancellationToken cancellationToken = default)
    {
        var collection = await GetCollectionAsync(collectionName, cancellationToken);
        
        if (collection.IsFailed)
        {
            return collection.ToResult();
        }
        
        return await execution(collection.Value);
    }
}