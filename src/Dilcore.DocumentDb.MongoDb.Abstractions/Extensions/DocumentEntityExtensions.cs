using MongoDB.Bson;

namespace Dilcore.DocumentDb.Abstractions.Extensions;

public static class DocumentEntityExtensions
{
    private const string UpdateSetOperator = "$set";
    
    public static BsonDocument ToBsonUpdateDocument<T>(this T document)
        where T : IDocumentEntity
    {
        var bsonDoc = new BsonDocument(UpdateSetOperator, document.ToBsonDocument());
        return bsonDoc;
    }
}