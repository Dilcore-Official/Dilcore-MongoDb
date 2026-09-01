using System.Text.Json;
using System.Text.Json.Nodes;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Json;
using FluentResults;
using MongoDB.Bson;

namespace Dilcore.MongoDB.SystemTextJson;

public static class JsonDocumentStoreExtensions
{
    public static Task<Result<BsonDocument>> InsertAsync(
        this JsonDocumentStore store,
        string logicalCollectionName,
        JsonDocument document,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(document);
        return InsertConvertedAsync(store, logicalCollectionName, document.ToBson(store.Converter, options), cancellationToken);
    }

    public static Task<Result<BsonDocument>> InsertAsync(
        this JsonDocumentStore store,
        string logicalCollectionName,
        JsonElement element,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        return InsertConvertedAsync(store, logicalCollectionName, element.ToBson(store.Converter, options), cancellationToken);
    }

    public static Task<Result<BsonDocument>> InsertAsync(
        this JsonDocumentStore store,
        string logicalCollectionName,
        JsonNode? node,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        return InsertConvertedAsync(store, logicalCollectionName, node.ToBson(store.Converter, options), cancellationToken);
    }

    public static async Task<Result<JsonDocument>> GetJsonDocumentByIdAsync(
        this JsonDocumentStore store,
        string logicalCollectionName,
        BsonValue id,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var document = await store.GetByIdAsync(logicalCollectionName, id, cancellationToken);
        if (document.IsFailed)
        {
            return document.ToResult();
        }

        return document.Value.ToJsonDocument(store.Converter, options);
    }

    public static async Task<Result<JsonElement>> GetJsonElementByIdAsync(
        this JsonDocumentStore store,
        string logicalCollectionName,
        BsonValue id,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var document = await store.GetByIdAsync(logicalCollectionName, id, cancellationToken);
        if (document.IsFailed)
        {
            return document.ToResult();
        }

        return document.Value.ToJsonElement(store.Converter, options);
    }

    private static Task<Result<BsonDocument>> InsertConvertedAsync(
        JsonDocumentStore store,
        string logicalCollectionName,
        Result<BsonValue> converted,
        CancellationToken cancellationToken)
    {
        if (converted.IsFailed)
        {
            return Task.FromResult(converted.ToResult<BsonDocument>());
        }

        if (converted.Value.BsonType != BsonType.Document)
        {
            return Task.FromResult(Result.Fail<BsonDocument>("JSON root must be a document."));
        }

        return store.InsertAsync(logicalCollectionName, converted.Value.AsBsonDocument, cancellationToken);
    }
}
