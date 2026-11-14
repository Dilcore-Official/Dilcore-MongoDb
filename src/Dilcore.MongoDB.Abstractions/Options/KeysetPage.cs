namespace Dilcore.MongoDB.Abstractions.Options;

/// <summary>
/// One page of keyset query results.
/// </summary>
/// <typeparam name="TDocument">Document type.</typeparam>
public sealed class KeysetPage<TDocument>
{
    /// <summary>
    /// Documents returned for this page.
    /// </summary>
    public required IReadOnlyList<TDocument> Items { get; init; }

    /// <summary>
    /// Continuation cursor when another page may exist; otherwise <see langword="null"/>.
    /// </summary>
    public string? NextCursor { get; init; }

    /// <summary>
    /// <see langword="true"/> when more items exist beyond this page.
    /// </summary>
    public bool HasMore { get; init; }
}