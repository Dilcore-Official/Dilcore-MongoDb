using System.Text.Json;
using System.Text.Json.Nodes;
using Dilcore.MongoDB.Abstractions.Json;
using FluentResults;
using MongoDB.Bson;

namespace Dilcore.MongoDB.SystemTextJson;

internal sealed class SystemTextJsonDocumentStore(IJsonDocumentStore store) : ISystemTextJsonDocumentStore
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
        JsonDocument document,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        store.InsertAsync(logicalCollectionName, document, options, cancellationToken);

    public Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        JsonElement element,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        store.InsertAsync(logicalCollectionName, element, options, cancellationToken);

    public Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        JsonNode? node,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        store.InsertAsync(logicalCollectionName, node, options, cancellationToken);

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

    public Task<Result<JsonDocument>> GetJsonDocumentByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        store.GetJsonDocumentByIdAsync(logicalCollectionName, id, options, cancellationToken);

    public Task<Result<JsonElement>> GetJsonElementByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        store.GetJsonElementByIdAsync(logicalCollectionName, id, options, cancellationToken);
}

internal sealed class SystemTextJsonDocumentStoreFactory(IJsonDocumentStoreFactory storeFactory)
    : ISystemTextJsonDocumentStoreFactory
{
    public ISystemTextJsonDocumentStore Create(
        Dilcore.MongoDB.Abstractions.Keys.MongoDatabaseKey databaseKey,
        string? staticPrefix = null) =>
        new SystemTextJsonDocumentStore(storeFactory.Create(databaseKey, staticPrefix));
}
