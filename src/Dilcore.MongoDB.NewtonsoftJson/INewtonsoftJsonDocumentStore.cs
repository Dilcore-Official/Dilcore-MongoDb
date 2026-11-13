using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using FluentResults;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dilcore.MongoDB.NewtonsoftJson;

/// <summary>
/// Newtonsoft.Json facade over <see cref="IJsonDocumentStore"/>.
/// </summary>
public interface INewtonsoftJsonDocumentStore
{
    IBsonJsonConverter Converter { get; }

    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        string json,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        JObject document,
        JsonBsonConversionOptions? options = null,
        JsonSerializerSettings? serializerSettings = null,
        CancellationToken cancellationToken = default);

    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        JToken token,
        JsonBsonConversionOptions? options = null,
        JsonSerializerSettings? serializerSettings = null,
        CancellationToken cancellationToken = default);

    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        BsonDocument document,
        CancellationToken cancellationToken = default);

    Task<Result<BsonDocument>> GetByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        CancellationToken cancellationToken = default);

    Task<Result<JObject>> GetJObjectByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<Result<JToken>> GetJTokenByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates <see cref="INewtonsoftJsonDocumentStore"/> instances bound to a database key.
/// </summary>
public interface INewtonsoftJsonDocumentStoreFactory
{
    INewtonsoftJsonDocumentStore Create(MongoDatabaseKey databaseKey, string? staticPrefix = null);
}
