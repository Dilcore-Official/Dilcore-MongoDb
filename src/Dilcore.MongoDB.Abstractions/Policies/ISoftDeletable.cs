namespace Dilcore.MongoDB.Abstractions.Policies;

/// <summary>
/// Opt-in soft-delete flag for a document.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// When <see langword="true"/>, the document is excluded from default reads and treated as deleted.
    /// </summary>
    bool IsDeleted { get; set; }
}