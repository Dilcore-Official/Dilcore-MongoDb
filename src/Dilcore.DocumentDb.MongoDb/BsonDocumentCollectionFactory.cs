using Dilcore.DocumentDb.Abstractions;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb;

public class BsonDocumentCollectionFactory(IMongoDbCollectionFactory mongoDbCollectionFactory)
    : IBsonDocumentCollectionFactory
{
    public async Task<Result<IMongoCollection<BsonDocument>>> GetCollectionAsync(string dbName, string collectionName, CancellationToken cancellationToken = default)
    {
        var collection = await mongoDbCollectionFactory.GetCollectionAsync(dbName, collectionName, cancellationToken);

        if (collection.IsFailed)
        {
            return collection.ToResult();
        }

        return collection;
    }
}