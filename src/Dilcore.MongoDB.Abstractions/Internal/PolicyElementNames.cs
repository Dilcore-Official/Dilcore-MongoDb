using Dilcore.MongoDB.Abstractions.Policies;
using MongoDB.Bson.Serialization;

namespace Dilcore.MongoDB.Abstractions.Internal;

internal static class PolicyElementNames
{
    public static string ETag<TDocument>()
        where TDocument : class
        => Element<TDocument>(nameof(IHasConcurrencyToken.ETag));

    public static string IsDeleted<TDocument>()
        where TDocument : class
        => Element<TDocument>(nameof(ISoftDeletable.IsDeleted));

    public static string UpdatedAt<TDocument>()
        where TDocument : class
        => Element<TDocument>(nameof(IAuditableDocument.UpdatedAt));

    private static string Element<TDocument>(string propertyName)
        where TDocument : class
    {
        var member = BsonClassMap.LookupClassMap(typeof(TDocument)).GetMemberMap(propertyName);
        if (member is not null)
        {
            return member.ElementName;
        }

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}