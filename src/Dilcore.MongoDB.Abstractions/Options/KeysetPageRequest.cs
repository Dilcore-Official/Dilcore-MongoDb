using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Options;

public sealed class KeysetPageRequest<TDocument>
    where TDocument : IDocumentEntity
{
    public required FilterDefinition<TDocument> Filter { get; init; }

    public required SortDefinition<TDocument> Sort { get; init; }

    public int PageSize { get; init; } = 50;

    public string? Cursor { get; init; }

    public ProjectionDefinition<TDocument>? Projection { get; init; }
}

public sealed class KeysetPage<TDocument>
{
    public required IReadOnlyList<TDocument> Items { get; init; }

    public string? NextCursor { get; init; }

    public bool HasMore { get; init; }
}
