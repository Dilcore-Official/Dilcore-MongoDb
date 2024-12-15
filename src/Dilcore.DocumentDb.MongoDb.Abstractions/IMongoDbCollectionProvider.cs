// ReSharper disable CheckNamespace

using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.Abstractions;

public interface IMongoDbCollectionProvider
{
    Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(
        Action<GetCollectionOptions<TDocument>> collectionOptions, CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity;
    
    Task<Result<IMongoCollection<BsonDocument>>> GetCollectionAsync(
        string databaseName, string collectionName, CancellationToken cancellationToken = default);
}