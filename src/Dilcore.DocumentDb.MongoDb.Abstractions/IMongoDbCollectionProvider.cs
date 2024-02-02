// ReSharper disable CheckNamespace

using FluentResults;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.Abstractions;

public interface IMongoDbCollectionProvider
{
    Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(
        Action<GetCollectionOptions<TDocument>> collectionOptions, CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity;
}