using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Json;
using FluentResults;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dilcore.MongoDB.NewtonsoftJson;

public static class JsonDocumentStoreExtensions
{
    public static Task<Result<BsonDocument>> InsertAsync(
        this JsonDocumentStore store,
        string logicalCollectionName,
        JObject document,
        JsonBsonConversionOptions? options = null,
        JsonSerializerSettings? serializerSettings = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(document);
        return InsertConvertedAsync(
            store,
            logicalCollectionName,
            document.ToBson(store.Converter, options, serializerSettings),
            cancellationToken);
    }

    public static Task<Result<BsonDocument>> InsertAsync(
        this JsonDocumentStore store,
        string logicalCollectionName,
        JToken token,
        JsonBsonConversionOptions? options = null,
        JsonSerializerSettings? serializerSettings = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(token);
        return InsertConvertedAsync(
            store,
            logicalCollectionName,
            token.ToBson(store.Converter, options, serializerSettings),
            cancellationToken);
    }

    public static async Task<Result<JObject>> GetJObjectByIdAsync(
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

        return document.Value.ToJObject(store.Converter, options);
    }

    public static async Task<Result<JToken>> GetJTokenByIdAsync(
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

        return document.Value.ToJToken(store.Converter, options);
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
