using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.Abstractions;

public interface IBsonDocumentCollectionFactory
{
    Task<Result<IMongoCollection<BsonDocument>>> GetCollectionAsync(string dbName, string collectionName,
        CancellationToken cancellationToken = default);
    
    Task<Result<string>> GetCollectionNameAsync(string dbName, string collectionName,
        CancellationToken cancellationToken = default);
}