using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using FluentResults;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dilcore.MongoDB.NewtonsoftJson;

internal sealed class NewtonsoftJsonDocumentStore(IJsonDocumentStore store) : INewtonsoftJsonDocumentStore
{
    public IBsonJsonConverter Converter => store.Converter;

    public Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        string json,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        store.InsertAsync(logicalCollectionName, json, options, cancellationToken);

    public Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        JObject document,
        JsonBsonConversionOptions? options = null,
        JsonSerializerSettings? serializerSettings = null,
        CancellationToken cancellationToken = default) =>
        store.InsertAsync(logicalCollectionName, document, options, serializerSettings, cancellationToken);

    public Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        JToken token,
        JsonBsonConversionOptions? options = null,
        JsonSerializerSettings? serializerSettings = null,
        CancellationToken cancellationToken = default) =>
        store.InsertAsync(logicalCollectionName, token, options, serializerSettings, cancellationToken);

    public Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        BsonDocument document,
        CancellationToken cancellationToken = default) =>
        store.InsertAsync(logicalCollectionName, document, cancellationToken);

    public Task<Result<BsonDocument>> GetByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        CancellationToken cancellationToken = default) =>
        store.GetByIdAsync(logicalCollectionName, id, cancellationToken);

    public Task<Result<JObject>> GetJObjectByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        store.GetJObjectByIdAsync(logicalCollectionName, id, options, cancellationToken);

    public Task<Result<JToken>> GetJTokenByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        store.GetJTokenByIdAsync(logicalCollectionName, id, options, cancellationToken);
}

internal sealed class NewtonsoftJsonDocumentStoreFactory(IJsonDocumentStoreFactory storeFactory)
    : INewtonsoftJsonDocumentStoreFactory
{
    public INewtonsoftJsonDocumentStore Create(MongoDatabaseKey databaseKey, string? staticPrefix = null) =>
        new NewtonsoftJsonDocumentStore(storeFactory.Create(databaseKey, staticPrefix));
}
