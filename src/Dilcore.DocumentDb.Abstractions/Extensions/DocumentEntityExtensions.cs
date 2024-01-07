using Dilcore.DocumentDb.Abstractions.Helpers;

namespace Dilcore.DocumentDb.Abstractions.Extensions;

public static class DocumentEntityExtensions
{
    public static void GenerateETag(this IDocumentEntity document)
    {
        document.ETag = DocumentDbHelper.GenerateEtag();
    }

    public static void UpdatedNow(this IDocumentEntity document)
    {
        document.UpdateAt = DateTime.UtcNow;
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
            throw new Exception("Id not provided");
        }
    }

    public static bool IsNew(this IDocumentEntity document)
    {
        return document.ETag.Equals(Constants.EmptyETag);
    }
}