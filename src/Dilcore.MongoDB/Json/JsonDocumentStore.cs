using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Results;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Json;

public sealed class JsonDocumentStore(
    IMongoDbCollectionFactory collectionFactory,
    IBsonJsonConverter converter,
    MongoDatabaseKey databaseKey,
    string? staticPrefix = null)
{
    public IBsonJsonConverter Converter { get; } = converter;

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
