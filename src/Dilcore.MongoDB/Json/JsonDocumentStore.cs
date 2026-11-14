using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Results;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Json;

/// <summary>
/// Untyped BSON document store for a specific database key, using the same namespace resolution as typed collections.
/// </summary>
/// <param name="collectionFactory">Resolves physical collections for logical names.</param>
/// <param name="converter">Parses JSON payloads before insert.</param>
/// <param name="databaseKey">The registered database this store targets.</param>
/// <param name="staticPrefix">Optional static namespace prefix override for collection resolution.</param>
public sealed class JsonDocumentStore(
    IMongoDbCollectionFactory collectionFactory,
    IBsonJsonConverter converter,
    MongoDatabaseKey databaseKey,
    string? staticPrefix = null) : IJsonDocumentStore
{
    /// <summary>
    /// The JSON converter used to parse string payloads before insert.
    /// </summary>
    public IBsonJsonConverter Converter { get; } = converter;

    /// <summary>
    /// Parses <paramref name="json"/> and inserts the resulting document into the resolved collection.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name within the bound database.</param>
    /// <param name="json">JSON object text; the root must be a document.</param>
    /// <param name="options">Optional parse options passed to <see cref="Converter"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The inserted document, or a failure from parsing, namespace resolution, or the driver.</returns>
    public async Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        string json,
        JsonBsonConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var parsed = Converter.Parse(json, options);
        if (parsed.IsFailed)
        {
            return parsed.ToResult();
        }

        if (parsed.Value.BsonType != BsonType.Document)
        {
            return Result.Fail<BsonDocument>("JSON root must be a document.");
        }

        return await InsertAsync(logicalCollectionName, parsed.Value.AsBsonDocument, cancellationToken);
    }

    /// <summary>
    /// Inserts a BSON document into the resolved collection.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name within the bound database.</param>
    /// <param name="document">The document to insert; must be under 16 MiB when serialized.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The inserted document, or a failure from validation, namespace resolution, or the driver.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is <see langword="null"/>.</exception>
    public async Task<Result<BsonDocument>> InsertAsync(
        string logicalCollectionName,
        BsonDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.ToBson().Length > 16 * 1024 * 1024)
        {
            return Result.Fail<BsonDocument>(new DocumentTooLargeError());
        }

        var collection = await collectionFactory.GetCollectionAsync(
            databaseKey, logicalCollectionName, staticPrefix, cancellationToken);
        if (collection.IsFailed)
        {
            return collection.ToResult();
        }

        try
        {
            await collection.Value.InsertOneAsync(document, cancellationToken: cancellationToken);
            return Result.Ok(document);
        }
        catch (MongoException exception)
        {
            return Abstractions.Internal.MongoExceptionMapper.Fail<BsonDocument>(exception);
        }
    }

    /// <summary>
    /// Loads a document by <c>_id</c> from the resolved collection.
    /// </summary>
    /// <param name="logicalCollectionName">Logical collection name within the bound database.</param>
    /// <param name="id">The BSON identifier value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document when found; otherwise <see cref="DocumentNotFoundError"/> or resolution errors.</returns>
    public async Task<Result<BsonDocument>> GetByIdAsync(
        string logicalCollectionName,
        BsonValue id,
        CancellationToken cancellationToken = default)
    {
        var collection = await collectionFactory.GetCollectionAsync(
            databaseKey, logicalCollectionName, staticPrefix, cancellationToken);
        if (collection.IsFailed)
        {
            return collection.ToResult();
        }

        var document = await collection.Value
            .Find(Builders<BsonDocument>.Filter.Eq("_id", id))
            .FirstOrDefaultAsync(cancellationToken);
        return document is null
            ? Result.Fail<BsonDocument>(new DocumentNotFoundError())
            : Result.Ok(document);
    }
}