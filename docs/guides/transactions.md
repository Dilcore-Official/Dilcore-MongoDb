# Commit several writes together in a transaction

Run writes across collections as one multi-document transaction with `IMongoDbTransactionRunner`. Package: `Dilcore.MongoDB`.

Transactions need a **replica set** (or mongos). A standalone `mongod` cannot run them: `ExecuteAsync` returns a failed `Result` instead of throwing.

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
        db.AddDocumentBinding<Order>("orders", d => d
            .WithCollectionName("orders")
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
        db.AddDocumentBinding<Payment>("payments", d => d
            .WithCollectionName("payments")
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
    }));

public sealed class Order : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = "";
}

public sealed class Payment : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
}
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Transactions;
using FluentResults;

public sealed class CheckoutService(IMongoDbTransactionRunner runner)
{
    public Task<Result<Guid>> PlaceOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        var clusterKey = new MongoClusterKey("primary");
        var options = new MongoTransactionOptions
        {
            MaxOperations = 100,                     // client-side budget, tighter than the 1000 default
            TimeLimit = TimeSpan.FromSeconds(10)
        };

        return runner.WithTransactionAsync(
            clusterKey,
            async (tx, ct) =>
            {
                // Take repositories from the context, not from DI, so the writes join this session.
                var orders = tx.Repositories.GetRepository<Order>("orders");
                var payments = tx.Repositories.GetRepository<Payment>("payments");

                // A retry runs this callback again. A fresh instance keeps Id and ETag unset
                // after a rolled-back insert, so the next attempt still takes the insert path.
                var attempt = new Order { Id = order.Id, Sku = order.Sku };
                var stored = await orders.StoreAsync(attempt, ct);
                if (stored.IsFailed)
                {
                    return stored.ToResult();            // a failed Result aborts the transaction
                }

                var paymentDocument = new Payment { OrderId = stored.Value.Id };
                var payment = await payments.StoreAsync(paymentDocument, ct);
                if (payment.IsFailed)
                {
                    return payment.ToResult();
                }

                return Result.Ok(stored.Value.Id);
            },
            options,
            cancellationToken);
    }
}
```

`IMongoDbTransactionRunner` has `ExecuteAsync` and `WithTransactionAsync` (an alias), each with a `Result` and a `Result<TResult>` overload. `IMongoDbTransactionContext` exposes `ClusterKey`, `Session` (the driver `IClientSessionHandle` for APIs the toolkit does not wrap) and `Repositories` (`IRepositoryResolver`, same binding keys as DI).

## Budgets

`MongoTransactionOptions` are **client-side estimates**, not MongoDB server limits:

| Option | Default | Meaning |
|--------|---------|---------|
| `MaxOperations` | 1000 | Tracked operations. A document write counts one, a bulk write one per model, a read one (and reserves 256 bytes). |
| `MaxEstimatedBytes` | 16 MiB | Sum of rendered BSON sizes of writes. Not a MongoDB "total transaction" cap. |
| `TimeLimit` | 60 s | Wall clock from runner start, including driver retries. Also checked when the callback returns. |
| `DriverOptions` | `null` | Passed to the driver `TransactionOptions`. |

MongoDB's own 16 MiB limit applies per document, and there is no server cap on the sum of a transaction.

## Handle failures

```csharp
using Dilcore.MongoDB.Abstractions.Results;

var result = await checkout.PlaceOrderAsync(order, cancellationToken);

if (result.HasError<TransactionBudgetExceededError>())
{
    // A client-side budget ran out. The transaction was aborted.
}
else if (result.HasError<CrossClusterOperationError>())
{
    // The callback used a repository bound to another cluster.
}
else if (result.IsFailed)
{
    // Includes a standalone server (no replica set) and any failed Result you returned.
}
```

A `MongoException` thrown from the callback before commit aborts the transaction and returns a failed `Result`, including when the error is not retryable. A failure during commit, such as `UnknownTransactionCommitResult` after the driver exhausts commit retries, also returns a failed `Result`, but the writes may already have committed. Do not repeat those writes until you know the outcome. An exception outside that mapping may propagate to the caller.

## Pitfalls

- Resolve repositories from `IMongoDbTransactionContext.Repositories`. A repository taken from the request scope does not join the session, so its writes survive an abort.
- Do not open a second runner or session inside the callback. A nested `ExecuteAsync` has its own session and can commit even when the outer callback fails.
- Keep operations sequential. Do not issue parallel operations on the same session.
- The driver can run the callback again after a `TransientTransactionError` (including a write conflict). Staged ETag and audit values are restored before the retry, but the callback must be safe to repeat. Do not send e-mails or call other systems inside it.
- Return a failed `Result` to abort. Do not rely on throwing.

## See also

- [Create, read and update documents](crud-with-repositories.md)
- [Driver escape hatches](../product/driver-escape-hatches.md)
- [Production MongoDB posture](../security/production-mongodb.md)
- [Sample](../../samples/MongoDb.Capabilities.Sample)
- Runnable example: [TransactionsExampleTests](../../test/Repositories.IntegrationTests/Examples/TransactionsExampleTests.cs)
