using Dilcore.MongoDB.Abstractions.Exceptions;
using Dilcore.MongoDB.Abstractions.Helpers;
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
        document.ETag = MongoDbHelper.GenerateEtag();
    }

    public static void CreatedNow(this IDocumentEntity document)
    {
        document.CreatedAt = DateTime.UtcNow;
    }

    public static void UpdatedNow(this IDocumentEntity document)
    {
        document.UpdatedAt = DateTime.UtcNow;
    }

    public static void NewId(this IDocumentEntity document)
    {
        document.Id = Guid.NewGuid();
    }

    public static bool IsIdEmpty(this IDocumentEntity document)
    {
        return document.Id.Equals(Guid.Empty);
    }

    public static void CheckId(this IDocumentEntity document)
    {
        if (document.IsIdEmpty())
        {
            throw new DocumentIdentifierIsEmptyException();
        }
    }

    public static bool IsNew(this IDocumentEntity document)
    {
        return document.ETag.Equals(Constants.EmptyETag);
    }

    public static BsonDocument ToBsonUpdateDocument<T>(this T document)
        where T : IDocumentEntity
    {
        _ = SerializationConfigured.Value;
        return new BsonDocument(UpdateSetOperator, document.ToBsonDocument());
    }

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
