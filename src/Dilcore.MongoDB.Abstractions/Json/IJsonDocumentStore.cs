using Dilcore.MongoDB.Abstractions.Keys;
using FluentResults;
using MongoDB.Bson;

namespace Dilcore.MongoDB.Abstractions.Json;

/// <summary>
/// Untyped JSON/BSON document store that resolves collections through the same
/// database and namespace pipeline as typed repositories.
/// </summary>
public interface IJsonDocumentStore
{
    IBsonJsonConverter Converter { get; }

    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        string json,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        BsonDocument document,
        CancellationToken cancellationToken = default);

    Task<Result<BsonDocument>> GetByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates <see cref="IJsonDocumentStore"/> instances bound to a database key.
/// </summary>
public interface IJsonDocumentStoreFactory
{
    IJsonDocumentStore Create(MongoDatabaseKey databaseKey, string? staticPrefix = null);
}
