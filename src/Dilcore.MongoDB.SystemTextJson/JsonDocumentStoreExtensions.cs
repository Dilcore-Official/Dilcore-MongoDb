using System.Text.Json;
using System.Text.Json.Nodes;
using Dilcore.MongoDB.Abstractions.Json;
using FluentResults;
using MongoDB.Bson;

namespace Dilcore.MongoDB.SystemTextJson;

/// <summary>
/// System.Text.Json overloads for <see cref="IJsonDocumentStore"/>.
/// </summary>
public static class JsonDocumentStoreExtensions
{
    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to BSON and inserts it through the store.
    /// </summary>
    /// <param name="store">The document store.</param>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="document">The JSON document to insert.</param>
    /// <param name="options">Optional conversion rules.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The inserted <see cref="BsonDocument"/>, or conversion or persistence errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> or <paramref name="document"/> is null.</exception>
    public static Task<Result<BsonDocument>> InsertAsync(
        this IJsonDocumentStore store,
        string logicalCollectionName,
        JsonDocument document,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(document);
        return InsertConvertedAsync(store, logicalCollectionName, document.ToBson(store.Converter, options), cancellationToken);
    }

    /// <summary>
    /// Converts a <see cref="JsonElement"/> to BSON and inserts it when the root is a document.
    /// </summary>
    /// <param name="store">The document store.</param>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="element">The JSON value to insert.</param>
    /// <param name="options">Optional conversion rules.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The inserted <see cref="BsonDocument"/>, or conversion or persistence errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is null.</exception>
    public static Task<Result<BsonDocument>> InsertAsync(
        this IJsonDocumentStore store,
        string logicalCollectionName,
        JsonElement element,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        return InsertConvertedAsync(store, logicalCollectionName, element.ToBson(store.Converter, options), cancellationToken);
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to BSON and inserts it when the root is a document.
    /// </summary>
    /// <param name="store">The document store.</param>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="node">The JSON node to insert, or null for BSON null.</param>
    /// <param name="options">Optional conversion rules.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The inserted <see cref="BsonDocument"/>, or conversion or persistence errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is null.</exception>
    public static Task<Result<BsonDocument>> InsertAsync(
        this IJsonDocumentStore store,
        string logicalCollectionName,
        JsonNode? node,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        return InsertConvertedAsync(store, logicalCollectionName, node.ToBson(store.Converter, options), cancellationToken);
    }

    /// <summary>
    /// Loads a document by <c>_id</c> and converts it to a <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="store">The document store.</param>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="options">Optional rules for BSON-to-JSON conversion.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A <see cref="JsonDocument"/> view of the document (caller owns disposal), or load or conversion errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is null.</exception>
    public static async Task<Result<JsonDocument>> GetJsonDocumentByIdAsync(
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

        return document.Value.ToJsonDocument(store.Converter, options);
    }

    /// <summary>
    /// Loads a document by <c>_id</c> and converts it to a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="store">The document store.</param>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="options">Optional rules for BSON-to-JSON conversion.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A cloned root <see cref="JsonElement"/>, or load or conversion errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> is null.</exception>
    public static async Task<Result<JsonElement>> GetJsonElementByIdAsync(
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

        return document.Value.ToJsonElement(store.Converter, options);
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