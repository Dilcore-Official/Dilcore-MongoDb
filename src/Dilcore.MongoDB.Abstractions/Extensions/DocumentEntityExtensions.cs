using Dilcore.MongoDB.Abstractions.Exceptions;
using Dilcore.MongoDB.Abstractions.Helpers;
using Dilcore.MongoDB.Abstractions.Internal;
using Dilcore.MongoDB.Abstractions.Policies;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Dilcore.MongoDB.Abstractions.Extensions;

public static class DocumentEntityExtensions
{
    private const string UpdateSetOperator = "$set";
    private static readonly Lazy<bool> SerializationConfigured = new(RegisterGuidSerializer);

    public static void GenerateETag(this IDocumentEntity document)
    {
        if (document is IHasConcurrencyToken concurrencyToken)
        {
            concurrencyToken.ETag = MongoDbHelper.GenerateEtag();
        }
    }

    public static void CreatedNow(this IDocumentEntity document)
    {
        if (document is IAuditableDocument auditable)
        {
            auditable.CreatedAt = DateTime.UtcNow;
        }
    }

    public static void UpdatedNow(this IDocumentEntity document)
    {
        if (document is IAuditableDocument auditable)
        {
            auditable.UpdatedAt = DateTime.UtcNow;
        }
    }

    public static void NewId<TDocument>(
        this TDocument document,
        GuidIdGenerationStrategy guidStrategy = GuidIdGenerationStrategy.Random)
        where TDocument : class, IDocumentEntity
    {
        DocumentIdAccessorCache.Get<TDocument>().EnsureNewId(document, guidStrategy);
    }

    public static bool IsIdEmpty<TDocument>(this TDocument document)
        where TDocument : class, IDocumentEntity
    {
        return DocumentIdAccessorCache.Get<TDocument>().IsEmpty(document);
    }

    public static void CheckId<TDocument>(this TDocument document)
        where TDocument : class, IDocumentEntity
    {
        if (document.IsIdEmpty())
        {
            throw new DocumentIdentifierIsEmptyException();
        }
    }

    public static bool IsNew(this IDocumentEntity document)
    {
        if (document is IHasConcurrencyToken concurrencyToken)
        {
            return concurrencyToken.ETag.Equals(Constants.EmptyETag);
        }

        // No concurrency token: create-vs-update is decided by whether Id is empty.
        return true;
    }

    /// <summary>
    /// Builds a <c>$set</c> of the serialized mutable snapshot, excluding <c>_id</c>.
    /// This overwrites every serialized field except the identifier. Prefer an explicit
    /// patch when only some fields change.
    /// </summary>
    public static BsonDocument ToBsonSnapshotUpdateDocument<T>(this T document)
        where T : IDocumentEntity
    {
        _ = SerializationConfigured.Value;
        var bson = document.ToBsonDocument();
        bson.Remove("_id");
        return new BsonDocument(UpdateSetOperator, bson);
    }

    /// <inheritdoc cref="ToBsonSnapshotUpdateDocument{T}"/>
    public static BsonDocument ToBsonUpdateDocument<T>(this T document)
        where T : IDocumentEntity
        => document.ToBsonSnapshotUpdateDocument();

    private static bool RegisterGuidSerializer()
    {
        try
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }
        catch (BsonSerializationException)
        {
            // Already registered by another component.
        }

        return true;
    }
}
