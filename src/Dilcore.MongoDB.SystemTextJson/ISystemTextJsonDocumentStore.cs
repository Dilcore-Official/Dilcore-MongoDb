using System.Text.Json;
using System.Text.Json.Nodes;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using FluentResults;
using MongoDB.Bson;

namespace Dilcore.MongoDB.SystemTextJson;

/// <summary>
/// System.Text.Json facade over <see cref="IJsonDocumentStore"/>.
/// </summary>
public interface ISystemTextJsonDocumentStore
{
    IBsonJsonConverter Converter { get; }

    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        string json,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        JsonDocument document,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        JsonElement element,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        JsonNode? node,
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

    Task<Result<JsonDocument>> GetJsonDocumentByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<Result<JsonElement>> GetJsonElementByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates <see cref="ISystemTextJsonDocumentStore"/> instances bound to a database key.
/// </summary>
public interface ISystemTextJsonDocumentStoreFactory
{
    ISystemTextJsonDocumentStore Create(MongoDatabaseKey databaseKey, string? staticPrefix = null);
}
