using System.Text.Json;
using System.Text.Json.Nodes;
using Dilcore.MongoDB.Abstractions.Json;
using FluentResults;
using MongoDB.Bson;

namespace Dilcore.MongoDB.SystemTextJson;

/// <summary>
/// System.Text.Json facade over <see cref="IJsonDocumentStore"/>.
/// </summary>
public interface ISystemTextJsonDocumentStore
{
    /// <summary>
    /// Gets the BSON/JSON converter shared with the underlying <see cref="IJsonDocumentStore"/>.
    /// </summary>
    IBsonJsonConverter Converter { get; }

    /// <summary>
    /// Parses JSON text and inserts the root document into the resolved collection.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="json">JSON object text to insert.</param>
    /// <param name="options">Optional conversion rules; when null, defaults apply.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The inserted <see cref="BsonDocument"/>, or errors from parsing, resolution, or persistence.</returns>
    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        string json,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to BSON and inserts it into the resolved collection.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="document">The JSON document to insert.</param>
    /// <param name="options">Optional conversion rules.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The inserted <see cref="BsonDocument"/>, or conversion or persistence errors.</returns>
    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        JsonDocument document,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a <see cref="JsonElement"/> to BSON and inserts it when the root is a document.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="element">The JSON value to insert.</param>
    /// <param name="options">Optional conversion rules.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The inserted <see cref="BsonDocument"/>, or conversion or persistence errors.</returns>
    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        JsonElement element,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to BSON and inserts it when the root is a document; null maps to BSON null.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="node">The JSON node to insert, or null for BSON null.</param>
    /// <param name="options">Optional conversion rules.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The inserted <see cref="BsonDocument"/>, or conversion or persistence errors.</returns>
    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        JsonNode? node,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts an existing <see cref="BsonDocument"/> into the resolved collection.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="document">The BSON document to insert.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The inserted document, or resolution or persistence errors.</returns>
    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        BsonDocument document,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a document by <c>_id</c> from the resolved collection.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching <see cref="BsonDocument"/>, or not-found, resolution, or driver errors.</returns>
    Task<Result<BsonDocument>> GetByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a document by <c>_id</c> and converts it to a <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="options">Optional rules for BSON-to-JSON conversion.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A <see cref="JsonDocument"/> view of the document (caller owns disposal), or load or conversion errors.</returns>
    Task<Result<JsonDocument>> GetJsonDocumentByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a document by <c>_id</c> and converts it to a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name used for namespace resolution.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="options">Optional rules for BSON-to-JSON conversion.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A cloned root <see cref="JsonElement"/>, or load or conversion errors.</returns>
    Task<Result<JsonElement>> GetJsonElementByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);
}