namespace Dilcore.MongoDB.Abstractions.Policies;

/// <summary>
/// Opt-in optimistic concurrency token (ETag) for a document.
/// </summary>
public interface IHasConcurrencyToken
{
    /// <summary>
    /// Monotonic concurrency token; <see cref="Constants.EmptyETag"/> means not yet persisted.
    /// </summary>
    long ETag { get; set; }
}