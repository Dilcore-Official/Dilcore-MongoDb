using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Options;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions;

public interface IMongoDbCollectionFactory
{
    Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(
        MongoDocumentBindingKey bindingKey,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity;

    Task<Result<IMongoCollection<BsonDocument>>> GetCollectionAsync(
        MongoDatabaseKey databaseKey,
        string logicalCollectionName,
        string? staticPrefix = null,
        CancellationToken cancellationToken = default);

    Task<Result<string>> ResolveCollectionNameAsync(
        MongoDatabaseKey databaseKey,
        string logicalCollectionName,
        string? staticPrefix = null,
        CancellationToken cancellationToken = default);
}
