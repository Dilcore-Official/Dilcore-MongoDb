namespace Dilcore.MongoDB.Abstractions.Policies;

/// <summary>
/// Opt-in create/update audit timestamps for a document.
/// </summary>
public interface IAuditableDocument
{
    /// <summary>
    /// UTC timestamp when the document was first stored.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp of the last update.
    /// </summary>
    DateTime UpdatedAt { get; set; }
}