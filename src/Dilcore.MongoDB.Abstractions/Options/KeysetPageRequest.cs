using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Options;

/// <summary>
/// Keyset (seek) paging request for <see cref="Repositories.IGenericRepository{TDocument}.GetPageAsync"/>.
/// </summary>
/// <typeparam name="TDocument">Document type.</typeparam>
public sealed class KeysetPageRequest<TDocument>
    where TDocument : IDocumentEntity
{
    /// <summary>
    /// Base filter applied before the keyset seek predicate.
    /// </summary>
    public required FilterDefinition<TDocument> Filter { get; init; }

    /// <summary>
    /// Sort that defines page ordering and cursor encoding (must be stable across pages).
    /// </summary>
    public required SortDefinition<TDocument> Sort { get; init; }

    /// <summary>
    /// Maximum documents per page (default 50).
    /// </summary>
    public int PageSize { get; init; } = 50;

    /// <summary>
    /// Opaque continuation token from a previous page's <see cref="KeysetPage{TDocument}.NextCursor"/>.
    /// </summary>
    public string? Cursor { get; init; }

    /// <summary>
    /// Optional field projection; sort keys must remain available to the server.
    /// </summary>
    public ProjectionDefinition<TDocument>? Projection { get; init; }
}