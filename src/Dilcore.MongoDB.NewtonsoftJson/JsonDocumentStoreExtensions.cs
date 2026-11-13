using Dilcore.MongoDB.Abstractions.Json;
using FluentResults;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dilcore.MongoDB.NewtonsoftJson;

/// <summary>
/// Newtonsoft.Json overloads for <see cref="IJsonDocumentStore"/>.
/// </summary>
public static class JsonDocumentStoreExtensions
{
    /// <summary>
    /// Converts a <see cref="JObject"/> to BSON and inserts it through the store.
    /// </summary>
    /// <param name="store">The document store.</param>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="document">The JSON object to insert.</param>
    /// <param name="options">Optional conversion rules.</param>
    /// <param name="serializerSettings">Optional serializer settings passed to BSON conversion.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The inserted <see cref="BsonDocument"/>, or conversion or persistence errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> or <paramref name="document"/> is null.</exception>
    public static Task<Result<BsonDocument>> InsertAsync(
        this IJsonDocumentStore store,
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

    /// <summary>
    /// Converts a <see cref="JToken"/> to BSON and inserts it when the root is a document.
    /// </summary>
    /// <param name="store">The document store.</param>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="token">The JSON value to insert.</param>
    /// <param name="options">Optional conversion rules.</param>
    /// <param name="serializerSettings">Optional serializer settings passed to BSON conversion.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The inserted <see cref="BsonDocument"/>, or conversion or persistence errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> or <paramref name="token"/> is null.</exception>
    public static Task<Result<BsonDocument>> InsertAsync(
        this IJsonDocumentStore store,
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

    /// <summary>
    /// Loads a document by <c>_id</c> and converts it to a <see cref="JObject"/>.
    /// </summary>
    /// <param name="store">The document store.</param>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="options">Optional rules for BSON-to-JSON conversion.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A <see cref="JObject"/> view of the document, or load or conversion errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is null.</exception>
    public static async Task<Result<JObject>> GetJObjectByIdAsync(
        this IJsonDocumentStore store,
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

    /// <summary>
    /// Loads a document by <c>_id</c> and converts it to a <see cref="JToken"/>.
    /// </summary>
    /// <param name="store">The document store.</param>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="options">Optional rules for BSON-to-JSON conversion.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A <see cref="JToken"/> view of the document root, or load or conversion errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is null.</exception>
    public static async Task<Result<JToken>> GetJTokenByIdAsync(
        this IJsonDocumentStore store,
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
        IJsonDocumentStore store,
        string logicalCollectionName,
        Result<BsonValue> converted,
        CancellationToken cancellationToken = default)
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