namespace Dilcore.DocumentDb.Abstractions;

public interface IDocumentEntity
{
    Guid Id { get; set; }
    long ETag { get; set; }
    bool IsDeleted { get; set; }
    DateTime UpdateAt { get; set; }
}