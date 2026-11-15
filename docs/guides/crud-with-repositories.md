# Create, read and update documents

Store, read and update typed documents with `IGenericRepository<TDocument>`. Package: `Dilcore.MongoDB`.

## Setup

```csharp
using System;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;

var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")!;
var services = new ServiceCollection();

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("shop", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<Product>("products", d => d
            .WithCollectionName("products")
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
    }));

public sealed class Product : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = "";
    public int Stock { get; set; }
}
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Repositories;
using FluentResults;
using MongoDB.Driver;

public sealed class ProductService(IGenericRepository<Product> products)
{
    // An empty Id inserts. A set Id updates the stored snapshot.
    public Task<Result<Product>> SaveAsync(Product product, CancellationToken cancellationToken = default)
        => products.StoreAsync(product, cancellationToken);

    // GetAsync(id) is an extension from Dilcore.MongoDB.Repositories.GenericRepositoryExtensions.
    public Task<Result<Product>> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => products.GetAsync(id, cancellationToken);

    public Task<Result<IReadOnlyList<Product>>> GetLowStockAsync(CancellationToken cancellationToken = default)
        => products.GetListAsync(x => x.Stock < 5, cancellationToken);

    // Replace swaps the whole stored document.
    public Task<Result<Product>> ReplaceAsync(Product changed, CancellationToken cancellationToken = default)
        => products.ReplaceAsync(changed, cancellationToken);

    // Patch applies only the update you pass. The filter decides which document is touched.
    public Task<Result<Product>> RestockAsync(Guid id, int amount, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Product>.Filter.Eq(x => x.Id, id);
        var update = Builders<Product>.Update.Inc(x => x.Stock, amount);

        return products.PatchAsync(filter, update, cancellationToken);
    }
}
```

Choose the update method on purpose:

| Method | Effect |
|--------|--------|
| `StoreAsync` | Insert when `Id` is empty. A caller-assigned id inserts when missing and updates the snapshot when present. |
| `ReplaceAsync` | Replaces the whole stored document. Extra stored fields that are not on the CLR type are dropped. |
| `UpdateSnapshotAsync` | `$set` of the mutable snapshot without `_id`. Unknown stored fields are kept. |
| `PatchAsync` | Applies only the `UpdateDefinition` you give. No full-document `$set` is invented. |

Other reads: `GetAsync<TDerived>`, `GetListAsync` overloads (filter, `FindOptions`, derived types), `HasAnyAsync`, `CountAsync`, `GetAsyncEnumerable` and [keyset paging](keyset-paging.md). `Dilcore.MongoDB.Repositories.GenericRepositoryExtensions` adds `GetAsync(id)`, `GetAsync(expression)`, `GetListAsync(expression)` and `DeleteAsync(expression)`.

## Handle failures

```csharp
using Dilcore.MongoDB.Abstractions.Results;

var found = await products.GetAsync(id, cancellationToken);
if (found.HasError<DocumentNotFoundError>())
{
    // No document matches. Not an exception.
}
```

| Error | When |
|-------|------|
| `DocumentNotFoundError` | `GetAsync` finds nothing; `ReplaceAsync` or `PatchAsync` matches nothing (without a concurrency token) |
| `DuplicateKeyError` | An insert or update violates a unique index |
| `TransientWriteError` | A retryable write failure whose outcome may be unknown. Retry only an idempotent operation, or read the current state before retrying |
| `ConcurrencyConflictError` | A stale ETag, see [Optimistic concurrency](optimistic-concurrency.md) |

The full table is in [Handle typed errors](handle-errors.md).

## Pitfalls

- Check `IsFailed` or `HasError<TError>()` before reading `Value`. Do not catch `MongoException` for expected failures.
- Always pass the `CancellationToken` into every repository call.
- `GetAsyncEnumerable` returns a stream, not a `Result`. It throws `CollectionResolutionException` when the collection cannot be resolved, and cancellation or driver exceptions surface while you enumerate. Dispose the enumerator (use `await foreach`).
- `PatchAsync` field names follow the document's BSON mapping, not hard-coded camelCase.

## See also

- [Define document types](define-documents.md)
- [Optimistic concurrency](optimistic-concurrency.md)
- [Soft delete](soft-delete.md)
- [Samples](../../samples/MongoDb.Capabilities.Sample)
- Runnable example: [CrudWithRepositoriesExampleTests](../../test/Repositories.IntegrationTests/Examples/CrudWithRepositoriesExampleTests.cs)
