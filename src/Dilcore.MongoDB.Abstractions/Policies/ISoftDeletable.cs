namespace Dilcore.MongoDB.Abstractions.Policies;

/// <summary>
/// Opt-in soft-delete flag for a document.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
