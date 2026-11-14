using Dilcore.MongoDB.Abstractions.Exceptions;
using Dilcore.MongoDB.Abstractions.Helpers;
using Dilcore.MongoDB.Abstractions.Internal;
using Dilcore.MongoDB.Abstractions.Policies;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Dilcore.MongoDB.Abstractions.Extensions;

/// <summary>
/// Helpers for document identity, audit/concurrency policies, and BSON update snapshots.
/// </summary>
public static class DocumentEntityExtensions
{
    private const string UpdateSetOperator = "$set";
    private static readonly Lazy<bool> SerializationConfigured = new(RegisterGuidSerializer);

    /// <summary>
    /// Assigns a new ETag when the document implements <see cref="IHasConcurrencyToken"/>.
    /// </summary>
    /// <param name="document">Target document.</param>
    public static void GenerateETag(this IDocumentEntity document)
    {
        if (document is IHasConcurrencyToken concurrencyToken)
        {
            concurrencyToken.ETag = MongoDbHelper.GenerateEtag();
        }
    }

    /// <summary>
    /// Sets <see cref="IAuditableDocument.CreatedAt"/> to the current UTC time when the document is auditable.
    /// </summary>
    /// <param name="document">Target document.</param>
    public static void CreatedNow(this IDocumentEntity document)
    {
        if (document is IAuditableDocument auditable)
        {
            auditable.CreatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Sets <see cref="IAuditableDocument.UpdatedAt"/> to the current UTC time when the document is auditable.
    /// </summary>
    /// <param name="document">Target document.</param>
    public static void UpdatedNow(this IDocumentEntity document)
    {
        if (document is IAuditableDocument auditable)
        {
            auditable.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Assigns a generated identifier when <typeparamref name="TDocument"/> implements <see cref="IDocumentEntity{TId}"/> and the current id is empty.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <param name="document">Target document.</param>
    /// <param name="guidStrategy">Guid generation mode when the identifier type is <see cref="Guid"/>.</param>
    /// <exception cref="UnsupportedIdentifierTypeException">The identifier type has no built-in generator.</exception>
    public static void NewId<TDocument>(
        this TDocument document,
        GuidIdGenerationStrategy guidStrategy = GuidIdGenerationStrategy.Random)
        where TDocument : class, IDocumentEntity
    {
        DocumentIdAccessorCache.Get<TDocument>().EnsureNewId(document, guidStrategy);
    }

    /// <summary>
    /// Returns whether the document's identifier is considered empty for its typed <c>Id</c>.
    /// </summary>
    /// <typeparam name="TDocument">Document type implementing <see cref="IDocumentEntity{TId}"/>.</typeparam>
    /// <param name="document">Target document.</param>
    /// <returns><see langword="true"/> when the identifier is empty; otherwise <see langword="false"/>.</returns>
    public static bool IsIdEmpty<TDocument>(this TDocument document)
        where TDocument : class, IDocumentEntity
    {
        return DocumentIdAccessorCache.Get<TDocument>().IsEmpty(document);
    }

    /// <summary>
    /// Throws <see cref="DocumentIdentifierIsEmptyException"/> when <see cref="IsIdEmpty{TDocument}"/> is <see langword="true"/>.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <param name="document">Target document.</param>
    /// <exception cref="DocumentIdentifierIsEmptyException">The identifier is empty.</exception>
    public static void CheckId<TDocument>(this TDocument document)
        where TDocument : class, IDocumentEntity
    {
        if (document.IsIdEmpty())
        {
            throw new DocumentIdentifierIsEmptyException();
        }
    }

    /// <summary>
    /// Indicates whether the document should be treated as a create rather than an update.
    /// </summary>
    /// <param name="document">Target document.</param>
    /// <returns>
    /// For <see cref="IHasConcurrencyToken"/> documents, <see langword="true"/> when <see cref="IHasConcurrencyToken.ETag"/> equals <see cref="Constants.EmptyETag"/>.
    /// Otherwise <see langword="true"/> (callers should use identifier emptiness separately).
    /// </returns>
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
        bson.Remove(DocumentElementNames.Id);
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