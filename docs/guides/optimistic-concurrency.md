# Guard updates with an ETag

Reject updates made from a stale copy of a document with `IHasConcurrencyToken`. Package: `Dilcore.MongoDB`.

## Setup

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;

public sealed class Invoice : IDocumentEntity<Guid>, IHasConcurrencyToken
{
    public Guid Id { get; set; }
    public long ETag { get; set; }      // managed by the repository
    public decimal Total { get; set; }
}

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("billing", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<Invoice>("invoices", d => d
            .WithCollectionName("invoices")
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
    }));
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Repositories;
using FluentResults;

public sealed class InvoiceService(IGenericRepository<Invoice> invoices)
{
    public async Task<Result<Invoice>> ReviseAsync(Guid id, decimal total, CancellationToken cancellationToken)
    {
        var current = await invoices.GetAsync(id, cancellationToken);
        if (current.IsFailed)
            return current;

        // The loaded ETag travels with the entity. A concurrent writer changes the stored ETag,
        // so this replace fails with ConcurrencyConflictError instead of overwriting their change.
        current.Value.Total = total;

        return await invoices.ReplaceAsync(current.Value, cancellationToken);
    }

    // Delete only when the caller still sees the current version.
    public Task<Result<bool>> DeleteAsync(Invoice seen, CancellationToken cancellationToken)
        => invoices.DeleteAsync(seen.Id, seen.ETag, cancellationToken);
}
```

The `ETag` is a non-zero random 64-bit token. The repository sets a new one on every successful write and stages it on your entity before the write (restored if the write fails).

## Handle failures

```csharp
using Dilcore.MongoDB.Abstractions.Results;

var result = await service.ReviseAsync(id, 10m, cancellationToken);
if (result.HasError<ConcurrencyConflictError>())
{
    // Stale copy. Re-read the document, reapply the change, and retry once.
}
```

| Operation | Stale ETag | Missing document |
|-----------|------------|------------------|
| `StoreAsync`, `ReplaceAsync`, `UpdateSnapshotAsync` | `ConcurrencyConflictError` | `DocumentNotFoundError` (`ReplaceAsync`) |
| `PatchAsync` with a concurrency token on the document type | `ConcurrencyConflictError` on a miss | - |
| `DeleteAsync(id, eTag)` | `ConcurrencyConflictError` | `DocumentNotFoundError` when no match without a token |

## Pitfalls

- Do not retry a conflict blindly. Re-read first, otherwise you loop on the same stale value.
- A caller that sends a zero or missing ETag for an existing document gets a conflict on every update.
- Bulk updates with an unmatched ETag are reported as failures, never as silent success. See [Bulk writes](bulk-writes.md).

## See also

- [Create, read and update documents](crud-with-repositories.md)
- [Soft delete](soft-delete.md)
- [Handle typed errors](handle-errors.md)
- Runnable example: [OptimisticConcurrencyExampleTests](../../test/Repositories.IntegrationTests/Examples/OptimisticConcurrencyExampleTests.cs)
