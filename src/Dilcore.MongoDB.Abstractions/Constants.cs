namespace Dilcore.MongoDB.Abstractions;

/// <summary>
/// Well-known constant values used by Dilcore.MongoDB policies and helpers.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Concurrency token value for documents that have not yet been persisted (see <see cref="Policies.IHasConcurrencyToken.ETag"/>).
    /// </summary>
    public const long EmptyETag = 0;
}