# Handle typed errors

Map expected MongoDB failures to your application's responses by error type instead of catching exceptions. Package: `Dilcore.MongoDB.Abstractions` (installed with `Dilcore.MongoDB`).

## Setup

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;

public sealed class Order : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = "";
}

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("shop", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<Order>("orders", d => d
            .WithCollectionName("orders")
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
    }));
```

## Use

Expected failures are values, not exceptions. Map them in one place:

```csharp
using Dilcore.MongoDB.Abstractions.Results;
using FluentResults;

public static class ResultStatus
{
    public static int ToStatusCode<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return 200;

        // Check the most specific error first when one result can carry several.
        if (result.HasError<DocumentNotFoundError>())
            return 404;

        // A stale ETag and a duplicate key both mean "the caller's view is out of date".
        if (result.HasError<ConcurrencyConflictError>() || result.HasError<DuplicateKeyError>())
            return 409;

        // Transient write errors are safe to retry. Tell the client so.
        if (result.HasError<TransientWriteError>())
            return 503;

        // Anything else is unexpected. Log result.Errors, do not leak them to the client.
        return 500;
    }
}

// In an ASP.NET Core endpoint: return Results.StatusCode(result.ToStatusCode());
```

Every error derives from `MongoOperationError` (FluentResults `Error`) and has a stable `Code`:

| Type | `Code` | Typical cause |
|------|--------|---------------|
| `DocumentNotFoundError` | `document_not_found` | Read, replace, delete or patch matched nothing |
| `ConcurrencyConflictError` | `concurrency_conflict` | Stale `ETag` |
| `DuplicateKeyError` | `duplicate_key` | Unique index or `_id` violation |
| `TransientWriteError` | `transient_write` | Retryable write failure |
| `WriteConcernFailureError` | `write_concern_failure` | Write concern not satisfied |
| `DocumentTooLargeError` | `document_too_large` | BSON document over the 16 MiB limit |
| `JsonConversionError` | `json_conversion` | Malformed or invalid JSON in a JSON adapter |
| `CrossClusterOperationError` | `cross_cluster_operation` | A transaction touched another cluster |
| `TransactionBudgetExceededError` | `transaction_budget_exceeded` | A client-side transaction budget ran out |
| `BulkWritePartialFailureError` | `bulk_write_partial_failure` | Some bulk items failed (see `Items`) |

All error types live in `Dilcore.MongoDB.Abstractions.Results`. Each error's cause and recommended action is in the [Typed error reference](error-reference.md).

## Handle failures

Branch on the type, or on `Code` when you cross a process boundary:

```csharp
var result = await orders.StoreAsync(order, cancellationToken);

var codes = result.Errors.OfType<MongoOperationError>().Select(e => e.Code);
```

Failures that are not `MongoOperationError` (for example an unknown database key or a missing tenant prefix) are plain FluentResults errors on a failed `Result`. Treat them as configuration or request-context problems, not as retryable.

## Pitfalls

- Do not call `.Value` on a `Result<T>` you have not checked. It throws on a failed result.
- Map by type, not by exception message text.
- A failed `Result` from a transaction callback aborts the transaction, see [Transactions](transactions.md).
- Do not log full documents that may hold personal data when you log `result.Errors`.

## See also

- [Typed error reference](error-reference.md)
- [Create, read and update documents](crud-with-repositories.md)
- [Optimistic concurrency](optimistic-concurrency.md)
- [Bulk writes](bulk-writes.md)
- Runnable example: [HandleErrorsExampleTests](../../test/Repositories.IntegrationTests/Examples/HandleErrorsExampleTests.cs)
