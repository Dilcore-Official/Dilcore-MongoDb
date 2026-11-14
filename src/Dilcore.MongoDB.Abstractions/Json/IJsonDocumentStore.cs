using FluentResults;
using MongoDB.Bson;

namespace Dilcore.MongoDB.Abstractions.Json;

/// <summary>
/// Untyped JSON/BSON document store that resolves collections through the same
/// database and namespace pipeline as typed repositories.
/// </summary>
public interface IJsonDocumentStore
{
    /// <summary>
    /// JSON converter used to parse insert payloads and format results.
    /// </summary>
    IBsonJsonConverter Converter { get; }

    /// <summary>
    /// Parses <paramref name="json"/> and inserts the root document into the resolved collection.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name.</param>
    /// <param name="json">JSON object to insert.</param>
    /// <param name="options">Optional parse options for <paramref name="json"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The inserted document, or resolution, conversion, or write errors.</returns>
    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        string json,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts <paramref name="document"/> into the resolved collection.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name.</param>
    /// <param name="document">BSON document to insert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The inserted document, or resolution or write errors.</returns>
    Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        BsonDocument document,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a document by <c>_id</c> from the resolved collection.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name.</param>
    /// <param name="id">Identifier value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document when found, or resolution or not-found errors.</returns>
    Task<Result<BsonDocument>> GetByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        CancellationToken cancellationToken = default);
}