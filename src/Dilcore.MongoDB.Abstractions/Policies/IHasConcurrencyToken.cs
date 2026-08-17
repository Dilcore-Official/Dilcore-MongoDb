namespace Dilcore.MongoDB.Abstractions.Policies;

/// <summary>
/// Opt-in optimistic concurrency token (ETag) for a document.
/// </summary>
public interface IHasConcurrencyToken
{
    long ETag { get; set; }
}
