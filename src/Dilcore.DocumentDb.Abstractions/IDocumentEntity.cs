namespace Dilcore.DocumentDb.Abstractions;

public interface IDocumentEntity
{
    Guid Id { get; set; }
    long ETag { get; set; }
    bool IsDeleted { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}