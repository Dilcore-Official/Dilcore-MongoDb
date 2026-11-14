# Page through results with keyset cursors

Read large result sets in stable pages that do not skip or repeat rows. Package: `Dilcore.MongoDB`.

## Setup

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;

public sealed class Post : IDocumentEntity<Guid>, IAuditableDocument
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Title { get; set; } = "";
}

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("blog", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<Post>("posts", d => d
            .WithCollectionName("posts")
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
    }));
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Repositories;
using FluentResults;
using MongoDB.Driver;

public sealed class PostService(IGenericRepository<Post> posts)
{
    public Task<Result<KeysetPage<Post>>> ListAsync(string? cursor, CancellationToken cancellationToken)
        => posts.GetPageAsync(
            new KeysetPageRequest<Post>
            {
                Filter = Builders<Post>.Filter.Empty,
                Sort = Builders<Post>.Sort.Descending(x => x.CreatedAt),   // the cursor is bound to this sort
                PageSize = 50,
                Cursor = cursor                                            // NextCursor of the previous page, or null
            },
            cancellationToken);
}

// The caller sends page.Value.NextCursor back while page.Value.HasMore is true.
```

`KeysetPage<T>` has `Items`, `NextCursor` and `HasMore`. The repository appends `_id` ascending as a tie-breaker when your sort does not include `_id`, so pages stay stable when sort values collide. Seeks follow MongoDB BSON type order for scalar values.

## Handle failures

```csharp
var page = await service.ListAsync(cursor, cancellationToken);
if (page.IsFailed)
{
    // A malformed cursor, a cursor from another sort, or an invalid request. Nothing is thrown.
}
```

A failed `Result` is returned for:

- a malformed cursor, or a cursor used with a different sort than the one that produced it;
- sort directions other than `1` or `-1` (`$meta` sorts are not supported);
- array-valued sort fields;
- a `Projection` that drops a sort key (including `_id`) or replaces one with a computed expression.

## Pitfalls

- Never reuse a cursor with a different sort. Start from `Cursor = null` when the sort changes.
- Treat the cursor as an opaque string. Do not parse or build one.
- Filters are applied on every page. Changing the filter between pages can skip or repeat rows.

## See also

- [Create, read and update documents](crud-with-repositories.md)
- [Projections](projections.md)
- [Handle typed errors](handle-errors.md)
- Runnable example: [KeysetPagingExampleTests](../../test/Repositories.IntegrationTests/Examples/KeysetPagingExampleTests.cs)
