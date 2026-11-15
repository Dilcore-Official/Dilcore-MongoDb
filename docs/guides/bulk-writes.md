# Write many documents at once

Insert, update and delete in batches with `IGenericBulkRepository<TDocument>`. Package: `Dilcore.MongoDB`.

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
    .AddDatabase("iot", db =>
    {
        db.OnCluster("primary");

        // The bulk repository is opt-in per binding.
        db.AddDocumentBinding<Reading>("readings", d => d
            .WithCollectionName("readings")
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
            .WithBulkRepository());
    }));

public sealed class Reading : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
    public string Sensor { get; set; } = "";
    public double Value { get; set; }
}
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using FluentResults;

public sealed class ReadingImporter(IGenericBulkRepository<Reading> bulk)
{
    public IReadOnlyList<int> FailedIndexes { get; private set; } = [];

    public async Task<Result> ImportAsync(
        IReadOnlyList<Reading> batch,
        CancellationToken cancellationToken = default)
    {
        // Unordered applies inside the current chunk: one failing item does not stop the other items in that chunk.
        // A write error still returns before later chunks are sent.
        var options = new MongoBulkWriteOptions { IsOrdered = false };

        var result = await bulk.BulkStoreRangeAsync(batch, options, cancellationToken);

        // Some items can succeed while others fail. The error lists every item by its index in the input.
        var partial = result.Errors.OfType<BulkWritePartialFailureError>().FirstOrDefault();
        var failed = partial is null
            ? []
            : partial.Items.Where(i => !i.Succeeded).Select(i => i.Index);
        FailedIndexes = failed.ToList();

        return result.ToResult();
    }

    public Task<Result> RemoveSensorAsync(string sensor, CancellationToken cancellationToken = default)
        => bulk.BulkDeleteAsync(x => x.Sensor == sensor, cancellationToken);
}
```

Members: `BulkStoreAsync(TDocument[])`, `BulkStoreRangeAsync(IEnumerable<TDocument>)` (both with an optional `MongoBulkWriteOptions`) and `BulkDeleteAsync(Expression<Func<TDocument, bool>>)`. `MongoBulkWriteOptions.IsOrdered` defaults to `true`; `MaxOperationsPerBatch` splits the input into chunks, and item indexes stay aligned with your input across chunks.

## Handle failures

| Error | Meaning |
|-------|---------|
| `BulkWritePartialFailureError` | Some items failed. `Items` holds a `BulkWriteItemResult` per item (`Index`, `Succeeded`, `ErrorMessage`). |
| `DocumentNotFoundError` | `BulkDeleteAsync` matched nothing. |

An update whose ETag no longer matches is a failed item, not a silent success. Entities whose writes committed keep their staged ETag and audit values; uncommitted items are restored.

## Pitfalls

- Inject `IGenericBulkRepository<T>` only after `WithBulkRepository()` is on the binding.
- With `IsOrdered = true` (the default) the first failure stops the batch, so later items are not written.
- `IsOrdered = false` continues past a failing item only inside the current chunk. A write error returns before later chunks are submitted, so the rest of the input is not attempted.
- Do not treat a failed `Result` as "nothing was written": check `BulkWritePartialFailureError` first.

## See also

- [Optimistic concurrency](optimistic-concurrency.md)
- [Handle typed errors](handle-errors.md)
- [Transactions](transactions.md)
- Runnable example: [BulkWritesExampleTests](../../test/Repositories.IntegrationTests/Examples/BulkWritesExampleTests.cs)
