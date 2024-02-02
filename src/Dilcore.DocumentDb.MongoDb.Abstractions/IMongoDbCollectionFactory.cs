using FluentResults;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.Abstractions;

public interface IMongoDbCollectionFactory
{
    Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(
        string dbName, CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity;

    Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(string dbName,
        Action<GetCollectionOptions<TDocument>> optionsAction,
        CancellationToken cancellationToken = default) where TDocument : class, IDocumentEntity;
}