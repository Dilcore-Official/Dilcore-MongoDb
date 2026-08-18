namespace Dilcore.MongoDB.Abstractions.Policies;

/// <summary>
/// Opt-in create/update audit timestamps for a document.
/// </summary>
public interface IAuditableDocument
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
